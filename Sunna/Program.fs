open System
open System.Net
open NLog
open Yggdrasil

let Logger = LogManager.GetLogger("Sunna")

[<EntryPoint>]
let main argv =
    let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    let login = API.CreateServerMailboxes loginServer
    login "roboco" "111111"
    Console.ReadKey() |> ignore 
    0 // return an integer exit code
