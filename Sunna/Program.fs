module Sunna.Main
open System
open System.Net
open FSharp.Control.Reactive
open Yggdrasil.Behavior
open Yggdrasil.Game
open Sunna.Machines
open Yggdrasil.Game.Components
open Yggdrasil.IO

let StartAgent credentials initialMachineState =
    //let inbox = EventHandler.SetupAgent () ()
    //let callback = Handshake.onReadyToConnect OnConnected
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let game = Handshake.Login server credentials
    printfn "Game started! %A" game
    Propagators.SetupPropagators game |> ignore
    //inbox.Post <|
    //        fun w -> {w with Inbox = inbox.Post}

[<EntryPoint>]
let main _ =
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args -> printfn "First change exception: %s: %s" AppDomain.CurrentDomain.FriendlyName args.Exception.Message
    StartAgent ("roboco", "111111") ()
    Console.ReadKey() |> ignore
    0
