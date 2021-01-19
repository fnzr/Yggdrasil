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
        Root: BehaviorTree.ActiveNode<World>
        Inbox: ('Data -> 'Data * 'Event[]) -> unit
        ActiveNode: BehaviorTree.ActiveNode<'Data>
    }
    static member Create root inbox =
        {Root=root;Inbox=inbox;ActiveNode=root}
        
module BehaviorTreeRunner =
    
    let Tick (runner: BehaviorTreeRunner<World, GameEvent>) world =
        match runner.ActiveNode world with
        | BehaviorTree.End status ->
            if status = BehaviorTree.Success then runner.Inbox <| fun w -> w, [|BehaviorResult Success|]
            else runner.Inbox <| fun w -> w, [|BehaviorResult Failure|]
            {runner with ActiveNode = runner.Root}
        | BehaviorTree.Next node ->
            {runner with ActiveNode = node}
                
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
                        | Some root -> Some (BehaviorTreeRunner<_, _>.Create root inbox)
                        
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
