module Yggdrasil.Behavior.Setup

open Yggdrasil.Agent
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Types

let rec AdvanceBehavior agent (stateMachine: StateMachine<State, AgentEvent, Agent>, tree) =
    let (queue, status) = BehaviorTree.Tick tree agent
    let next = match status with
                | BehaviorTree.Success -> stateMachine.TryTransit BehaviorTreeSuccess agent
                | BehaviorTree.Failure -> stateMachine.TryTransit BehaviorTreeFailure agent
                | _ -> None
    match next with
    | Some machine ->
        (machine, BehaviorTree.InitTreeOrEmpty machine.CurrentState.Behavior)
            |> AdvanceBehavior agent
    | None -> stateMachine, queue

let EventMailbox (agent: Agent) stateMachine (inbox: MailboxProcessor<AgentEvent>) =
    let rec loop (currentMachine: StateMachine<State, AgentEvent, Agent>) currentTree = async {
        let! event = inbox.Receive()
        
        if event = AgentEvent.MapChanged then agent.Dispatcher DoneLoadingMap
        
        let machine, queue = 
            match currentMachine.TryTransit event agent with
                | Some m -> m, BehaviorTree.InitTreeOrEmpty m.CurrentState.Behavior
                | None -> currentMachine, currentTree
            |> AdvanceBehavior agent
        return! loop machine queue
    }
    let bt = FSharpx.Collections.Queue.empty
    loop stateMachine bt
    
let StartAgent stateMachine =
    let agent = Agent ()
    
    let mailbox = MailboxProcessor.Start(EventMailbox agent stateMachine)
    mailbox.Error.Add <| Logger.Error

    agent.OnEventDispatched.Add(mailbox.Post)
    agent.Location.OnEventDispatched.Add(mailbox.Post)
    agent.Inventory.OnEventDispatched.Add(mailbox.Post)
    agent.BattleParameters.OnEventDispatched.Add(mailbox.Post)
    agent.Level.OnEventDispatched.Add(mailbox.Post)
    agent.Health.OnEventDispatched.Add(mailbox.Post)
    agent.Goals.OnEventDispatched.Add(mailbox.Post)
    
    stateMachine.Start agent