open System
open System.Net
open NLog
open Yggdrasil.Behavior.Machines
open Yggdrasil.Behavior.Setup
let Logger = LogManager.GetLogger("Sunna")

[<EntryPoint>]
let main argv =
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let machine = DefaultMachine.Create server "roboco" "111111" 
    StartAgent machine
    //Machines.DefaultMachine server "roboco" "111111"
    Console.ReadKey() |> ignore 
    0 // return an integer exit code
