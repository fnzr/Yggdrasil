open System
open System.Net
open NLog
open Yggdrasil

let Logger = LogManager.GetCurrentClassLogger()

let BehaviorFactory id = ()

let rec ReadInput office =
    let line = Console.ReadLine ()
    API.PostReport office <| line.Split (' ')
    ReadInput office

[<EntryPoint>]
let main argv =
    let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    Logger.Debug "heeey"
    let (office, login) = API.CreateServerOffice loginServer BehaviorFactory   
    login "roboco" "111111"    
    ReadInput office
    0 // return an integer exit code
