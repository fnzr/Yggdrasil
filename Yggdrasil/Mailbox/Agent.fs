module Yggdrasil.Mailbox.Agent

open NLog
open Yggdrasil.Communication
open Yggdrasil.PacketTypes
open Yggdrasil.YggrasilTypes
open Yggdrasil.Mailbox.Publisher

let Logger = LogManager.GetCurrentClassLogger()

let OnU32ParameterUpdate code value (agent: Agent) =
    match code with
    | Parameter.Weight -> agent.Weight <- value
    | Parameter.MaxWeight -> agent.MaxWeight <- value
    | Parameter.SkillPoints -> agent.SkillPoints <- value
    | Parameter.JobLevel -> agent.JobLevel <- value
    | Parameter.BaseLevel -> agent.BaseLevel <- value
    | Parameter.MaxHP -> agent.MaxHP <- value
    | Parameter.MaxSP -> agent.MaxSP <- value
    | Parameter.SP -> agent.SP <- value
    | Parameter.HP -> agent.HP <- value
    | _ -> ()
    
let OnI16ParameterUpdate code value (agent: Agent) =
    match code with
    //| Parameter.Manner -> agent.Manner <- value
    | Parameter.Hit -> agent.Hit <- value
    | Parameter.Flee1 -> agent.Flee1 <- value
    | Parameter.Flee2 -> agent.Flee2 <- value
    | Parameter.Critical -> agent.Critical <- value
    | _ -> ()
    
let OnU16ParameterUpdate code value (agent: Agent) =
    match code with
    | Parameter.Speed -> agent.Speed <- value
    | Parameter.AttackSpeed -> agent.AttackSpeed <- value
    | Parameter.Attack1 -> agent.Attack1 <- value
    | Parameter.Attack2 -> agent.Attack2 <- value
    | Parameter.Defense1 -> agent.Defense1 <- value
    | Parameter.Defense2 -> agent.Defense2 <- value
    | Parameter.MagicAttack1 -> agent.MagicAttack1 <- value
    | Parameter.MagicAttack2 -> agent.MagicAttack2 <- value
    | Parameter.MagicDefense1 -> agent.MagicDefense1 <- value
    | Parameter.MagicDefense2 -> agent.MagicDefense2 <- value
    | Parameter.AttackRange -> agent.AttackRange <- value
    | _ -> ()
    
let OnI32ParameterUpdate code value (agent: Agent) =
    match code with
    | Parameter.Zeny -> agent.Zeny <- value
    | Parameter.USTR -> agent.STRUpgradeCost <- value
    | Parameter.UAGI -> agent.AGIUpgradeCost <- value
    | Parameter.UDEX -> agent.DEXUpgradeCost <- value
    | Parameter.UVIT -> agent.VITUpgradeCost <- value
    | Parameter.ULUK -> agent.LUKUpgradeCost <- value
    | Parameter.UINT -> agent.INTUpgradeCost <- value
    | _ -> ()

let On64ParameterUpdate code value agent =
    match code with
    | Parameter.BaseExp -> agent.BaseExp <- value
    | Parameter.JobExp -> agent.JobExp <- value
    | Parameter.NextBaseExp -> agent.NextBaseExp <- value
    | Parameter.NextJobExp -> agent.NextJobExp <- value
    | _ -> ()
    
let OnPairParameterUpdate code value (agent: Agent) =
    match code with
    | Parameter.STR -> agent.STRRaw <- value
    | Parameter.AGI -> agent.AGIRaw <- value
    | Parameter.DEX -> agent.DEXRaw <- value
    | Parameter.VIT -> agent.VITRaw <- value
    | Parameter.LUK -> agent.LUKRaw <- value
    | Parameter.INT -> agent.INTRaw <- value
    | _ -> ()
    
let CreateAgent accountId =
    {
        AccountId=accountId;CharacterName="";BaseLevel=0u;JobLevel=0u
        HP=0u;MaxHP=0u;SP=0u;MaxSP=0u;BaseExp=0L;JobExp=0L
        NextBaseExp=0L;NextJobExp=0L;StatusPoints=0u;SkillPoints=0u
        Weight=0u;MaxWeight=0u;Zeny=0;STRRaw=(0us,0s);AGIRaw=(0us,0s)
        VITRaw=(0us,0s);INTRaw=(0us,0s);DEXRaw=(0us,0s);LUKRaw=(0us,0s)
        AttackRange=0us;AttackSpeed=0us;Attack1=0us;Attack2=0us;MagicAttack1=0us
        MagicAttack2=0us;Defense1=0us;Defense2=0us;MagicDefense1=0us
        MagicDefense2=0us;Hit=0s;Flee1=0s;Flee2=0s;Critical=0s;Speed=0us
        STRUpgradeCost=0;AGIUpgradeCost=0;VITUpgradeCost=0;INTUpgradeCost=0
        DEXUpgradeCost=0;LUKUpgradeCost=0
    }
    
let HandleEvent agent msg =
    match msg with
    | StatusU32 (p, v) ->
        match p with
        | Parameter.HP ->
            let old = agent.HP
            agent.HP <- v
            Some(HealthChanged (old, v))
        | _ -> OnU32ParameterUpdate p v agent; Some(ParameterChanged p)
    | StatusI32 (p, v) -> OnI32ParameterUpdate p v agent; Some(ParameterChanged p)
    | StatusU16 (p, v) -> OnU16ParameterUpdate p v agent; Some(ParameterChanged p)
    | StatusI16 (p, v) -> OnI16ParameterUpdate p v agent; Some(ParameterChanged p)
    | StatusPair (p, v1, v2) -> OnPairParameterUpdate p (v1, v2) agent; Some(ParameterChanged p)
    | Status64 (p, v) -> On64ParameterUpdate p v agent; Some(ParameterChanged p)
    | Name n -> agent.CharacterName <- n; None
    | AccountId id -> agent.AccountId <- id; None
    | Print -> printfn "%A" agent; None
        

let CreateAgentMailbox accountId (publisher: PublisherMailbox) =
    MailboxProcessor.Start(
        fun (inbox: MailboxProcessor<AgentUpdate>) ->
        let agent = CreateAgent accountId
        let handler = HandleEvent agent
        let rec loop () = async {
            let! msg = inbox.Receive()            
            match handler msg with
            | Some event -> publisher.Post <| Publish (event, agent)
            | None -> ()
            return! loop()
        }
        loop() 
    )
