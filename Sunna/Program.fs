module Sunna.Main
open System
open System.Net
open Yggdrasil.Behavior
open Yggdrasil.Game
open Sunna.Machines

let OnlineLogin world =
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let (user, pass) = world.Credentials
    Yggdrasil.IO.Handshake.Login server user pass world.Inbox

let StartAgent credentials initialMachineState =
    let world = {Game.Default
                 with
                    Login = OnlineLogin
                    Credentials = credentials}
    let inbox = Agent.SetupAgent world initialMachineState
    inbox.Post <|
        fun w -> {w with Inbox = inbox.Post}

[<EntryPoint>]
let main _ =
    Async.Start <| async { Server.StartServer() }
    //Console.ReadKey() |> ignore
    StartAgent ("roboco", "111111") (DefaultMachine.Create())
    Console.ReadKey() |> ignore
    0
