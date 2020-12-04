open System
open System.Diagnostics
open System.Net
open NLog
open Yggdrasil

let Logger = LogManager.GetCurrentClassLogger()

let BehaviorFactory id = ()

[<EntryPoint>]
let main argv =
    let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    let (mailboxes, login) = API.CreateServerMailboxes loginServer BehaviorFactory
    //printfn "%d" Stopwatch.Frequency
    login "roboco" "111111"
    API.CommandLineHandler mailboxes 
    0 // return an integer exit code
