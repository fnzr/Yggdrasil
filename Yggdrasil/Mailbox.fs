module Yggdrasil.Mailbox

open System.Timers
open Yggdrasil.Types

let ProcessMessage (agent: Agent) message =
    match message with
    | Position p -> {agent with Position = p}
    | Destination d -> {agent with Destination = d}    
    | Inventory i -> {agent with Inventory = i}
    | BattleParameters p -> {agent with BattleParameters = p}
    | Level l -> {agent with Level = l}
    | NewSkill s -> {agent with Skills = s :: agent.Skills}
    | HPSP hs -> {agent with HPSP = hs}
    | Map m -> {agent with Map = m}
    | ConnectionAccepted -> {agent with IsConnected = true}
    | GetState channel -> channel.Reply(agent); agent
    | Ping -> agent
    
let Ping (mailbox: MailboxProcessor<StateMessage>) _ = mailbox.Post Ping
    
let MailboxFactory name map dispatcher =    
    MailboxProcessor.Start(
        fun inbox ->
            let rec loop agent = async {
                let! msg = inbox.Receive()
                let newAgent = ProcessMessage agent msg
                return! loop newAgent
            }
            let timer = new Timer(1000.0)
            timer.Elapsed.Add(Ping inbox)
            timer.Enabled <- true
            timer.AutoReset <- true
            let initialAgent = Agent.Create name map inbox dispatcher
            //let initialTreeState = behaviorTree.Start initialAgent 
            loop initialAgent //initialTreeState
    )