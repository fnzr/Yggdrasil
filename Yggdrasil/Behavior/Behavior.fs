module Yggdrasil.Behavior.Behavior

open System
open System.Net
open NLog
open Yggdrasil.Behavior.Machines
open Yggdrasil.Behavior.Trees
open Yggdrasil.Game
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Utils
open Yggdrasil.Game.Event

let Logger = LogManager.GetLogger "Behavior"

type BehaviorTreeRunner<'Data, 'Event> =
    {
        Root: BehaviorTree.Factory<'Data>
        Inbox: ('Data -> 'Data * 'Event[]) -> unit
        Blackboard: Map<BehaviorTree.MapKey, obj>
        Queue: FSharpx.Collections.Queue<BehaviorTree.Node<'Data>>
    }
    
    static member Create root inbox =
        {Root=root;Inbox=inbox;Blackboard=Map.empty;Queue=FSharpx.Collections.Queue.empty}
        
module BehaviorTreeRunner =
    
    let Tick (runner: BehaviorTreeRunner<World, GameEvent>) data =
        let _runner =
            if runner.Queue.IsEmpty then {runner with Queue = BehaviorTree.InitTree runner.Root}
            else runner
        let (q, status, bb) = BehaviorTree.Tick _runner.Queue data _runner.Blackboard
        let blackboard = 
            match status with
                | BehaviorTree.Status.Success -> (_runner.Inbox <| fun w -> w, [|BehaviorResult Success|]); bb
                | BehaviorTree.Status.Failure -> (_runner.Inbox <| fun w -> w, [|BehaviorResult Failure|]); bb
                | BehaviorTree.Status.Running ->
                    match Map.tryFind (RequestPing :> BehaviorTree.MapKey) bb with
                    | None -> bb
                    | Some value ->
                        Delay (fun () ->
                            _runner.Inbox <| (fun w -> w, [|BehaviorResult Success|])) (Convert.ToInt32 value)
                        Map.remove (RequestPing :> BehaviorTree.MapKey) bb
                | _ -> bb
        {_runner with
            Blackboard = blackboard
            Queue = q}
                
let rec MoveState world inbox events (machine: StateMachine<'State, World>, runner) =
    match events with
    | [||] -> machine, runner
    | _ ->
        MoveState world inbox events.[1..] <|
            match machine.TryTransit (BuildUnionKey events.[0]) world with
            | None -> machine, runner
            | Some m -> m,
                        match m.CurrentState.Behavior with
                        | None -> None
                        | Some root -> Some <| BehaviorTreeRunner.Create<World, GameEvent> root inbox
                        
let EventMailbox (inbox: MailboxProcessor<World -> World * GameEvent[]>) =
    let server =  IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let machine = DefaultMachine.Create server "roboco" "111111" inbox.Post
    let rec loop (currentWorld: World) (currentMachine: StateMachine<'State, World>)
        (currentRunner: BehaviorTreeRunner<World, GameEvent> option) = async {
        let! event = inbox.Receive()
        let (world, events) = event currentWorld
        Logger.Debug ("Events: {es}", events)        
        
        let machine, runner = MoveState world inbox.Post events (currentMachine, currentRunner)
        let nextRunner =
            if runner.IsSome then Some <| BehaviorTreeRunner.Tick runner.Value world
            else runner
        return! loop world machine nextRunner
    }
    machine.Start World.Default
    loop World.Default machine None
    
let StartAgent stateMachine =
    let mailbox = MailboxProcessor.Start(EventMailbox)
    mailbox.Error.Add <| Logger.Error
