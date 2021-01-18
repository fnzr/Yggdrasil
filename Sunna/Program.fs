open System
open System.Net
open Yggdrasil.Behavior.Machines

[<EntryPoint>]
let main _ =
    //let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    Yggdrasil.Behavior.Behavior.StartAgent ()
    Console.ReadKey() |> ignore
    //let map = Yggdrasil.Navigation.Maps.GetMapData "prontera"
    //let path = Yggdrasil.Navigation.Pathfinding.FindPath map (155, 33) (156, 22) 0
    //printfn "%A" path
    0
