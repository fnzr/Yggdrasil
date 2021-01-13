module Yggdrasil.Behavior.Setup

open System.Collections.Generic
open System.Timers
open Yggdrasil.Agent.Agent
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Agent.Event

type BehaviorTreeRunner(root: BehaviorTree.Factory<Agent>, agent: Agent) as this =
    let timer = new Timer(150.0)
    do timer.Enabled <- true
    do timer.AutoReset <- true
    do timer.Elapsed.Add(this.Tick)
    let mutable queue = FSharpx.Collections.Queue.empty
    let data = Dictionary<string, obj>()
    member this.Data = data
    member this.Restart () = timer.Enabled <- true
    member this.Tick _ =
        if queue.IsEmpty then
            queue <- BehaviorTree.InitTree root
        let (q, status) = BehaviorTree.Tick queue agent data
        match status with
            | BehaviorTree.Status.Success ->
                agent.Publish <| BehaviorResult Success
                timer.Enabled <- false
            | BehaviorTree.Status.Failure ->
                agent.Publish <| BehaviorResult Failure
                timer.Enabled <- false
            | _ -> ()
        queue <- q

let EventMailbox (agent: Agent) stateMachine (inbox: MailboxProcessor<GameEvent>) =
    let rec loop (currentMachine: StateMachine<'State, GameEvent, Agent>) (tree: BehaviorTreeRunner option) = async {
        let! event = inbox.Receive()
        
        let machine, behavior = 
            match currentMachine.TryTransit event agent with
                | Some m ->
                    m,
                    match m.CurrentState.Behavior with
                    | None -> None
                    | Some r -> Some <| BehaviorTreeRunner(r, agent)                    
                | None -> currentMachine, tree
        
        match event with
        | BehaviorResult _ ->
            match behavior with
            | Some b -> b.Restart()
            | None -> ()
        | Connection Inactive ->
            match behavior with
            | Some b -> () //stop ticks
            | None -> ()
        | _ -> ()
        return! loop machine behavior
    }
    loop stateMachine None
    
let StartAgent stateMachine =
    let agent = Agent ()
    
    let mailbox = MailboxProcessor.Start(EventMailbox agent stateMachine)
    mailbox.Error.Add <| Logger.Error
    agent.OnEventDispatched.Add(mailbox.Post)
    stateMachine.Start agent