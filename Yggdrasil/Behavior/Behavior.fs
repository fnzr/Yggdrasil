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

type BehaviorTreeRunner<'data> =
    {
        Root: BehaviorTree.ActiveNode<'data>
        ActiveNode: BehaviorTree.ActiveNode<'data>
    }
    static member Create root = {Root=root;ActiveNode=root}
    static member NoOp =
        let rec noop _ = BehaviorTree.Next noop
        {Root=noop;ActiveNode=noop}
    member this.Tick world =
        match this.ActiveNode world with
        | BehaviorTree.End status ->
            {this with ActiveNode = this.Root}, Some status
        | BehaviorTree.Next node ->
            {this with ActiveNode = node}, None
        
let rec MoveState world inbox events (machine: StateMachine<_, _>) =
    match events with
    | [] -> machine
    | e::es ->
        MoveState world inbox es <|
            match machine.TryTransit (BuildUnionKey e) world with
            | None -> machine
            | Some m -> m
            
let AdvanceBehavior world inbox machine (runner: BehaviorTreeRunner<_>) =
    let (_runner, result) = runner.Tick world
    match result with
    | None -> machine, _runner
    | Some s ->
        let event = if s = BehaviorTree.Success
                    then BehaviorResult.Success else BehaviorResult.Failure
        let _machine = MoveState world inbox [event] machine
        _machine, match _machine.CurrentState.Behavior with
                    | None -> BehaviorTreeRunner<_>.NoOp
                    | Some b -> BehaviorTreeRunner<_>.Create b
    
                        
let EventMailbox (inbox: MailboxProcessor<World -> World * GameEvent[]>) =
    let server =  IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let machine = DefaultMachine.Create server "roboco" "111111" inbox.Post
    let rec loop (currentWorld: World) (currentMachine: StateMachine<_, _>)
        (currentRunner: BehaviorTreeRunner<_>) = async {
        let! event = inbox.Receive()
        let (world, events) = event currentWorld
        Logger.Debug ("Events: {es}", events)        
        
        let (_machine, _runner) =
            MoveState world inbox.Post (Array.toList events) currentMachine |>
            fun m -> if m = currentMachine then m, currentRunner
                     else m, match m.CurrentState.Behavior with
                             | Some b -> BehaviorTreeRunner<_>.Create b
                             | None -> BehaviorTreeRunner<_>.NoOp
        
        let (nextMachine, nextRunner) = AdvanceBehavior world inbox.Post _machine _runner
            
        return! loop world nextMachine nextRunner
    }
    machine.Start World.Default
    loop World.Default machine (BehaviorTreeRunner<_>.NoOp)
    
let StartAgent stateMachine =
    let mailbox = MailboxProcessor.Start(EventMailbox)
    mailbox.Error.Add Logger.Error
