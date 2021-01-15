module Yggdrasil.Behavior.Behavior

open System.Collections.Generic
open NLog
open Yggdrasil.Game
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Game.Event

let Logger = LogManager.GetLogger "Behavior"

type BehaviorTreeRunner(root: BehaviorTree.Factory<Game>, inbox: MailboxProcessor<GameEvent>) =
    let mutable queue = FSharpx.Collections.Queue.empty
    let data = Dictionary<string, obj>()
    member this.Tick agent =
        if queue.IsEmpty then
            queue <- BehaviorTree.InitTree root
        let (q, status) = BehaviorTree.Tick queue agent data
        match status with
            | BehaviorTree.Status.Success -> inbox.Post <| BehaviorResult Success
            | BehaviorTree.Status.Failure -> inbox.Post <| BehaviorResult Failure
            | _ -> ()
        queue <- q

let EventMailbox (stateMachine: StateMachine<'State, Game>) (inbox: MailboxProcessor<GameEvent>) =
    let game = {
        World = World(inbox)
        Connection = Connection(inbox)
    }
    stateMachine.Start game
    let rec loop (currentMachine: StateMachine<'State, Game>) (currentBehavior: BehaviorTreeRunner option) = async {
        let! event = inbox.Receive()
        
        let key = BuildUnionKey event
        //Logger.Debug ("Event: {key:s}", key)
        let machine, behavior = 
            match currentMachine.TryTransit key game with
                | Some m ->
                    m,
                    match m.CurrentState.Behavior with
                    | None -> None
                    | Some root -> Some <| BehaviorTreeRunner(root, inbox)                    
                | None -> currentMachine, currentBehavior
        
        if behavior.IsSome then behavior.Value.Tick game
        return! loop machine behavior
    }
    loop stateMachine None
    
let StartAgent stateMachine =
    let mailbox = MailboxProcessor.Start(EventMailbox stateMachine)
    mailbox.Error.Add <| Logger.Error
