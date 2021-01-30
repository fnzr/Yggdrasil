module Sunna.Main
open System
open System.Net
open Yggdrasil.Behavior
open Yggdrasil.Game
open Sunna.Machines
open Yggdrasil.Game.Components
open Yggdrasil.IO

let OnConnected game =
    Propagators.SetupPropagators game |> ignore

let StartAgent credentials initialMachineState =
    //let inbox = EventHandler.SetupAgent () ()
    let callback = Handshake.onReadyToConnect OnConnected
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    Handshake.Login server credentials callback
    //inbox.Post <|
    //        fun w -> {w with Inbox = inbox.Post}

[<EntryPoint>]
let main _ =
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args -> printfn "First change exception: %s: %s" AppDomain.CurrentDomain.FriendlyName args.Exception.Message
    //Async.Start <| async { Server.StartServer() }
    StartAgent ("roboco", "111111") ()
    Console.ReadKey() |> ignore
    0
