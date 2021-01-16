open System
open System.Net
//open Yggdrasil.Behavior.Machines

[<EntryPoint>]
let main _ =
    //let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    //let machine = DefaultMachine.Create server "roboco" "111111" 
    //Yggdrasil.Behavior.Behavior.StartAgent machine
    let a = [|1; 2; 3|]    
    Async.Start <| async {
        let b = a.[2..]
        do! Async.Sleep 2000
        printfn "%A" b
    }
    a.[2] <- 0
    Console.ReadKey() |> ignore
    //let map = Yggdrasil.Navigation.Maps.GetMapData "prontera"
    //let path = Yggdrasil.Navigation.Pathfinding.FindPath map (155, 33) (156, 22) 0
    //printfn "%A" path
    0
