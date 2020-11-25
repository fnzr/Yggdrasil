module Yggdrasil.Agent

open NLog
open Yggdrasil.PacketTypes
open Yggdrasil.YggrasilTypes
open Yggdrasil.Publisher

let Logger = LogManager.GetCurrentClassLogger()

let On32StatusUpdate code value (attributes, character, primary, secondary, extra) =
    match code with
    | 0us -> secondary.Speed <- value; None
    | 3us -> extra.Karma <- value; None
    | 4us -> extra.Manner <- value; None
    | 5us -> character.HP <- value; Some(HealthChanged)
    | 6us -> character.MaxHP <- value; None
    | 7us -> character.SP <- value; None
    | 8us -> character.MaxSP <- value; None
    | 9us -> primary.StatusPoints <- value; None
    | 11us -> character.BaseLevel <- value; None
    | 12us -> primary.SkillPoints <- value; None
    | 13us -> attributes.STR <- value; None
    | 14us -> attributes.AGI <- value; None
    | 15us -> attributes.VIT <- value; None
    | 16us -> attributes.INT <- value; None
    | 17us -> attributes.DEX <- value; None
    | 18us -> attributes.LUK <- value; None
    | 20us -> primary.Zeny <- value; None
    | 24us -> primary.Weight <- value; None
    | 25us -> extra.MaxWeight <- value; None
    | 41us -> secondary.Attack1 <- value; None
    | 42us -> secondary.Attack2 <- value; None
    | 43us -> secondary.MagicAttack1 <- value; None
    | 44us -> secondary.MagicAttack2 <- value; None
    | 45us -> secondary.Defense1 <- value; None
    | 46us -> secondary.Defense2 <- value; None
    | 47us -> secondary.MagicDefense1 <- value; None
    | 48us -> secondary.MagicDefense2 <- value; None
    | 49us -> secondary.Hit <- value; None
    | 50us -> secondary.Flee1 <- value; None
    | 51us -> secondary.Flee2 <- value; None
    | 52us -> secondary.Critical <- value; None
    | 53us -> secondary.AttackSpeed <- value; None
    | 55us -> character.JobLevel <- value; None
    | 1000us -> secondary.AttackRange <- value; None
    | _ -> Logger.Warn("Unhandled status: {status}", code); None
    
let On64StatusUpdate code value (_, _, primary, _, _) =
    match code with
    | 1us -> primary.BaseExp <- value; None
    | 2us -> primary.JobExp <- value; None
    | 22us -> primary.NextBaseExp <- value; None
    | 23us -> primary.NextJobExp <- value; None
    | _ -> Logger.Warn("Unhandled status64: {status}", code); None
    
let CreateAgent accountId =
    {STR=0;AGI=0;VIT=0;INT=0;DEX=0;LUK=0},
    {AccountId=accountId;BaseLevel=0;JobLevel=0;HP=0;MaxHP=0;SP=0;MaxSP=0},
    {BaseExp=0L;JobExp=0L;NextBaseExp=0L;NextJobExp=0L;StatusPoints=0;SkillPoints=0;Weight=0;Zeny=0},
    {AttackRange=0;AttackSpeed=0;Attack1=0;Attack2=0;MagicAttack1=0;MagicAttack2=0;Defense1=0;Defense2=0;MagicDefense1=0;MagicDefense2=0;Hit=0;Flee1=0;Flee2=0;Critical=0;Speed=0},
    {MaxWeight=0;Karma=0;Manner=0}
        
let CreateAgentMessageHandler accountId =
    MailboxProcessor.Start(
        fun (inbox: MailboxProcessor<Message>) ->
        let agent = CreateAgent accountId
        let rec loop() = async {
            let! msg = inbox.Receive()
            let optEvent = match msg with
                            | StatusUpdate (c, v) -> On32StatusUpdate c v agent
                            | Status64Update (c, v) -> On64StatusUpdate c v agent
            match optEvent with
            | Some event -> Publish <| event agent
            | None -> ()
            return! loop()
        }
        loop()
    )
