module Yggdrasil.Mailbox

open System.Timers
open Yggdrasil.Behavior
open Yggdrasil.Behavior.StateMachine
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
    
let MailboxFactory name map dispatcher machineState =    
    MailboxProcessor.Start(
        let MachineTick = Tick Machines.TransitionsMap
        fun inbox ->
            let rec loop agent machine = async {
                let! msg = inbox.Receive()
                let newAgent = ProcessMessage agent msg
                let newMachine = MachineTick machine newAgent
                return! loop newAgent newMachine
            }
            let timer = new Timer(1000.0)
            timer.Elapsed.Add(Ping inbox)
            timer.Enabled <- true
            timer.AutoReset <- true
            let initialAgent = Agent.Create name map inbox dispatcher
            let activeState = ActiveState<Agent>.Create machineState
            //let initialTreeState = behaviorTree.Start initialAgent 
            loop initialAgent activeState //initialTreeState
    )