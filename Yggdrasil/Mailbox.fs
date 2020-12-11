module Yggdrasil.Mailbox

open Yggdrasil.Types

let ProcessMessage agent message =
    match message with
    | Position p -> {agent with Position = p}
    | Destination d -> {agent with Destination = d}
    | Inventory i -> {agent with Inventory = i}
    | BattleParameters p -> {agent with BattleParameters = p}
    | Level l -> {agent with Level = l}
    | NewSkill s -> {agent with Skills = s :: agent.Skills}
    | HPSP hs -> {agent with HPSP = hs}
    | Map m -> {agent with Map = m}
    | Name n -> {agent with Name = n}
    | GetState channel -> channel.Reply(agent); agent 
    
let BehaviorFactory () =
    MailboxProcessor.Start(
        fun inbox ->
            let rec loop agent = async {
                let! msg = inbox.Receive()
                let newAgent = ProcessMessage agent msg
                if agent.Position = (0,0) && agent.Position <> newAgent.Position then
                    printfn "Starting position: %A" newAgent.Position
                return! loop newAgent
            }
            loop Agent.Default
    )