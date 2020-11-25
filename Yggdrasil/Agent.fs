module Yggdrasil.Agent

open NLog
open Yggdrasil.PacketTypes
open Yggdrasil.YggrasilTypes
open Yggdrasil.Publisher

let Logger = LogManager.GetCurrentClassLogger()

let On32StatusUpdate code value agent =
    match code with
    | StatusCode.Speed -> agent.CombatStatus.Speed <- value; None
    | StatusCode.Karma -> None //Karma
    | StatusCode.Manner -> None //Manner
    | StatusCode.HP ->
        let old = agent.CharacterStatus.HP
        agent.CharacterStatus.HP <- value
        Some(HealthChanged (old, value))
    | StatusCode.MaxHP -> agent.CharacterStatus.MaxHP <- value; None
    | StatusCode.SP -> agent.CharacterStatus.SP <- value; None
    | StatusCode.MaxSP -> agent.CharacterStatus.MaxSP <- value; None
    | StatusCode.StatusPoints -> agent.CharacterStatus.StatusPoints <- value; None
    | StatusCode.BaseLevel -> agent.CharacterStatus.BaseLevel <- value; None
    | StatusCode.SkillPoints -> agent.CharacterStatus.SkillPoints <- value; None
    | StatusCode.STR -> agent.Attributes.STR <- value; None
    | StatusCode.AGI -> agent.Attributes.AGI <- value; None
    | StatusCode.VIT -> agent.Attributes.VIT <- value; None
    | StatusCode.INT -> agent.Attributes.INT <- value; None
    | StatusCode.DEX -> agent.Attributes.DEX <- value; None
    | StatusCode.LUK -> agent.Attributes.LUK <- value; None
    | StatusCode.Zeny -> agent.CharacterStatus.Zeny <- value; None
    | StatusCode.Weight -> agent.CharacterStatus.Weight <- value; None
    | StatusCode.MaxWeight -> None
    | StatusCode.Attack1 -> agent.CombatStatus.Attack1 <- value; None
    | StatusCode.Attack2 -> agent.CombatStatus.Attack2 <- value; None
    | StatusCode.MagicAttack1 -> agent.CombatStatus.MagicAttack1 <- value; None
    | StatusCode.MagicAttack2 -> agent.CombatStatus.MagicAttack2 <- value; None
    | StatusCode.Defense1 -> agent.CombatStatus.Defense1 <- value; None
    | StatusCode.Defense2 -> agent.CombatStatus.Defense2 <- value; None
    | StatusCode.MagicDefense1 -> agent.CombatStatus.MagicDefense1 <- value; None
    | StatusCode.MagicDefense2 -> agent.CombatStatus.MagicDefense2 <- value; None
    | StatusCode.Hit -> agent.CombatStatus.Hit <- value; None
    | StatusCode.Flee1 -> agent.CombatStatus.Flee1 <- value; None
    | StatusCode.Flee2 -> agent.CombatStatus.Flee2 <- value; None
    | StatusCode.Critical -> agent.CombatStatus.Critical <- value; None
    | StatusCode.AttackSpeed -> agent.CombatStatus.AttackSpeed <- value; None
    | StatusCode.JobLevel -> agent.CharacterStatus.JobLevel <- value; None
    | StatusCode.AttackRange -> agent.CombatStatus.AttackRange <- value; None
    | _ -> Logger.Warn("Unhandled status32: {status}", code); None
    
let On64StatusUpdate code value agent =
    match code with
    | StatusCode.BaseExp -> agent.CharacterStatus.BaseExp <- value; None
    | StatusCode.JobExp -> agent.CharacterStatus.JobExp <- value; None
    | StatusCode.NextBaseExp -> agent.CharacterStatus.NextBaseExp <- value; None
    | StatusCode.NextJobExp -> agent.CharacterStatus.NextJobExp <- value; None
    | _ -> Logger.Warn("Unhandled status64: {status}", code); None
    
let CreateAgent accountId =
    {
        AccountId = accountId
        Attributes = {STR=0;AGI=0;VIT=0;INT=0;DEX=0;LUK=0}
        CharacterStatus = {BaseLevel=0;JobLevel=0;HP=0;MaxHP=0;SP=0;MaxSP=0;BaseExp=0L;JobExp=0L;NextBaseExp=0L;NextJobExp=0L;StatusPoints=0;SkillPoints=0;Weight=0;Zeny=0}
        CombatStatus = {AttackRange=0;AttackSpeed=0;Attack1=0;Attack2=0;MagicAttack1=0;MagicAttack2=0;Defense1=0;Defense2=0;MagicDefense1=0;MagicDefense2=0;Hit=0;Flee1=0;Flee2=0;Critical=0;Speed=0}
    }
        
let CreateAgentMailbox accountId =
    MailboxProcessor.Start(
        fun (inbox: MailboxProcessor<Message>) ->
        let rec loop agent = async {
            let! msg = inbox.Receive()
            let optEvent = match msg with
                            | StatusUpdate (c, v) -> On32StatusUpdate c v agent
                            | Status64Update (c, v) -> On64StatusUpdate c v agent
                            | Print -> printfn "%A" agent; None
            match optEvent with
            | Some event -> () //Publish <| (event, agent)
            | None -> ()
            return! loop agent
        }
        loop <| CreateAgent accountId
    )
