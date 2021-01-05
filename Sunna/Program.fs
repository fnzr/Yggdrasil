open System
open System.Net
open NLog
open Yggdrasil.Agent
open Yggdrasil.Behavior
open Yggdrasil.Behavior.StateMachine
let Logger = LogManager.GetLogger("Sunna")

[<EntryPoint>]
let main argv =
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    StartAgent server "roboco" "111111" Machines.DefaultMachine
    //Machines.DefaultMachine server "roboco" "111111"
    Console.ReadKey() |> ignore 
    0 // return an integer exit code
