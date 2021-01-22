module Sunna.Main
open System
open Yggdrasil.Game
open Sunna.Machines

let StartAgent credentials machineFactory =
    let world = {World.Default
                 with Player =
                        {Player.Default with Credentials = credentials}}
    let inbox = Yggdrasil.Behavior.Agent.SetupAgent world machineFactory
    inbox.Post <|
        fun w -> {w with Inbox = inbox.Post}, []

[<EntryPoint>]
let main _ =
    //let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    StartAgent ("roboco", "111111") DefaultMachine.Create
    Console.ReadKey() |> ignore
    //let map = Yggdrasil.Navigation.Maps.GetMapData "prontera"
    //let path = Yggdrasil.Navigation.Pathfinding.FindPath map (155, 33) (156, 22) 0
    //printfn "%A" path
    0
