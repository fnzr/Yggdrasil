module Yggdrasil.Behavior.Setup

open Yggdrasil.Agent
open Yggdrasil.Behavior.StateMachine

(*
let rec AdvanceBehavior agent (stateMachine: StateMachine<State, GameEvent, Agent>, tree) =
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
*)
let EventMailbox (agent: Agent) stateMachine (inbox: MailboxProcessor<GameEvent>) =
    let rec loop (currentMachine: StateMachine<'State, GameEvent, Agent>) = async {
        let! event = inbox.Receive()
        
        let machine = 
            match currentMachine.TryTransit event agent with
                | Some m ->
                    match m.CurrentState.Behavior with
                    | None -> agent.BehaviorTree <- None
                    | Some r -> agent.BehaviorTree <- Some <| AgentBehaviorTree(r, agent)
                    m
                | None -> currentMachine
        
        match event with
        | BehaviorResult _ ->
            match agent.BehaviorTree with
            | Some b -> b.Restart()
            | None -> ()
        | _ -> ()
        return! loop machine
    }
    loop stateMachine
    
let StartAgent stateMachine =
    let agent = Agent ()
    
    let mailbox = MailboxProcessor.Start(EventMailbox agent stateMachine)
    mailbox.Error.Add <| Logger.Error
    agent.OnEventDispatched.Add(mailbox.Post)
    stateMachine.Start agent