module Yggdrasil.Behavior.Behavior

open System
open System.Net
open NLog
open FSharpPlus.Lens
open Yggdrasil.Behavior.Machines
open Yggdrasil.Game
open Yggdrasil.Behavior.FSM
open Yggdrasil.Utils
open Yggdrasil.Game.Event

let Logger = LogManager.GetLogger "Behavior"

let HandleEvents (events: _[]) data state =
    match events with
    | [||] -> state
    | es -> State.Handle es.[0] data state
            

let EventMailbox credentials (inbox: MailboxProcessor<World -> World * WorldEvent[]>) =
    let rec loop (currentWorld: World) (currentState: ActiveState<_,_,_>) = async {
        let! update = inbox.Receive()
        let (world, events) = update currentWorld
        Logger.Debug ("Events: {es}", events)
            
        let state =
            currentState
            |> HandleEvents events world
            |> State.Tick world
        return! loop world state
        
    }
    let world =
        {World.Default
            with Inbox = inbox.Post
                 Player = {Player.Default with Credentials = credentials}}
    loop world (DefaultMachine.Create() |> State.Tick world)
    
let StartAgent credentials =
    let mailbox = MailboxProcessor.Start(EventMailbox credentials)
    mailbox.Error.Add Logger.Error
