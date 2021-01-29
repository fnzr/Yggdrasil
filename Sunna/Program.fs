module Sunna.Main
open System
open System.Net
open Yggdrasil.Behavior
open Yggdrasil.Game
open Sunna.Machines
open Yggdrasil.Game.Components
open Yggdrasil.IO
(*
let timer = new System.Timers.Timer(500.0)
timer.AutoReset <- true

// events are automatically IObservable
let timeStream = timer.Elapsed

type InventoryCell =
    {
        InventoryChangedEvent: Event<Inventory>
    }
    [<CLIEvent>]
    member this.InventoryChanged = this.InventoryChangedEvent.Publish

let InventoryCellInstance =
    {
      InventoryChangedEvent = Event<Inventory>()
    }
let a =
    InventoryCellInstance.InventoryChanged
    |> Observable.map (fun i -> i.Weight = i.MaxWeight)
// Inventory -> Boolean
let IsAtMaxWeight (e: IObservable<Inventory>) =
    e
    |> Observable.map (fun i -> i.Weight = i.MaxWeight)
*)


let StartAgent credentials initialMachineState =
    let inbox = EventHandler.SetupAgent () ()
    let callback = Handshake.onReadyToConnect inbox.Post
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    Handshake.Login server credentials callback
    //inbox.Post <|
    //        fun w -> {w with Inbox = inbox.Post}

[<EntryPoint>]
let main _ =
    Async.Start <| async { Server.StartServer() }
    //Console.ReadKey() |> ignore
    StartAgent ("roboco", "111111") ()
    Console.ReadKey() |> ignore
    0
