module Yggdrasil.Agent

open NLog
open Yggdrasil.PacketTypes
open Yggdrasil.YggrasilTypes
open Yggdrasil.Publisher

let Logger = LogManager.GetCurrentClassLogger()

let On32StatusUpdate code value agent =
    match code with
    | 0us -> agent.CombatStatus.Speed <- value; None
    | 3us -> None //Karma
    | 4us -> None //Manner
    | 5us -> agent.CharacterStatus.HP <- value; Some(HealthChanged)
    | 6us -> agent.CharacterStatus.MaxHP <- value; None
    | 7us -> agent.CharacterStatus.SP <- value; None
    | 8us -> agent.CharacterStatus.MaxSP <- value; None
    | 9us -> agent.CharacterStatus.StatusPoints <- value; None
    | 11us -> agent.CharacterStatus.BaseLevel <- value; None
    | 12us -> agent.CharacterStatus.SkillPoints <- value; None
    | 13us -> agent.Attributes.STR <- value; None
    | 14us -> agent.Attributes.AGI <- value; None
    | 15us -> agent.Attributes.VIT <- value; None
    | 16us -> agent.Attributes.INT <- value; None
    | 17us -> agent.Attributes.DEX <- value; None
    | 18us -> agent.Attributes.LUK <- value; None
    | 20us -> agent.CharacterStatus.Zeny <- value; None
    | 24us -> agent.CharacterStatus.Weight <- value; None
    | 25us -> None //MaxWeight
    | 41us -> agent.CombatStatus.Attack1 <- value; None
    | 42us -> agent.CombatStatus.Attack2 <- value; None
    | 43us -> agent.CombatStatus.MagicAttack1 <- value; None
    | 44us -> agent.CombatStatus.MagicAttack2 <- value; None
    | 45us -> agent.CombatStatus.Defense1 <- value; None
    | 46us -> agent.CombatStatus.Defense2 <- value; None
    | 47us -> agent.CombatStatus.MagicDefense1 <- value; None
    | 48us -> agent.CombatStatus.MagicDefense2 <- value; None
    | 49us -> agent.CombatStatus.Hit <- value; None
    | 50us -> agent.CombatStatus.Flee1 <- value; None
    | 51us -> agent.CombatStatus.Flee2 <- value; None
    | 52us -> agent.CombatStatus.Critical <- value; None
    | 53us -> agent.CombatStatus.AttackSpeed <- value; None
    | 55us -> agent.CharacterStatus.JobLevel <- value; None
    | 1000us -> agent.CombatStatus.AttackRange <- value; None
    | _ -> Logger.Warn("Unhandled status32: {status}", code); None
    
let On64StatusUpdate code value agent =
    match code with
    | 1us -> agent.CharacterStatus.BaseExp <- value; None
    | 2us -> agent.CharacterStatus.JobExp <- value; None
    | 22us -> agent.CharacterStatus.NextBaseExp <- value; None
    | 23us -> agent.CharacterStatus.NextJobExp <- value; None
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
        let agent = CreateAgent accountId
        let rec loop() = async {
            let! msg = inbox.Receive()
            let optEvent = match msg with
                            | StatusUpdate (c, v) -> On32StatusUpdate c v agent
                            | Status64Update (c, v) -> On64StatusUpdate c v agent
                            | Print -> printfn "%A" agent; None
            match optEvent with
            | Some event -> Publish <| event agent
            | None -> ()
            return! loop()
        }
        loop()
    )
