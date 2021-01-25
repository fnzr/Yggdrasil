module Sunna.Main
open System
open System.Diagnostics
open System.Net
open NLog
open NLog.Config
open Yggdrasil.Behavior
open Yggdrasil.Game
open Yggdrasil.Behavior.BehaviorTree
open Sunna.Machines
open Sunna.Trees

let OnlineLogin world =
    let server = IPEndPoint (IPAddress.Parse "192.168.2.10", 6900)
    let (user, pass) = world.Player.Credentials
    Yggdrasil.IO.Handshake.Login server user pass world.Inbox

let StartAgent credentials initialMachineState =
    let world = {World.Default
                 with
                    Login = OnlineLogin
                    Player =
                        {Player.Default with Credentials = credentials}}
    let inbox = Agent.SetupAgent world initialMachineState
    inbox.Post <|
        fun w -> {w with Inbox = inbox.Post}

[<EntryPoint>]
let main _ =
    Async.Start <| async { Sunna.Server.StartServer() }
    Console.ReadKey() |> ignore
    //LogManager.Setup().SetupExtensions(
     //   fun s -> s.RegisterTarget<JsonLogger.MyFirstTarget>("first") |> ignore) |> ignore
    //ConfigurationItemFactory.Default.Targets.RegisterDefinition("first", typeof<JsonLogger.MyFirstTarget>)
    //StartAgent ("roboco", "111111") (DefaultMachine.Create())
    let rec a () = 
        LogManager.GetLogger("mylogger").Info("hello world")
        Console.ReadKey() |> ignore
        a ()
    a ()
    //let map = Yggdrasil.Navigation.Maps.GetMapData "prontera"
    //let path = Yggdrasil.Navigation.Pathfinding.FindPath map (155, 33) (156, 22) 0
    //printfn "%A" path
    0
