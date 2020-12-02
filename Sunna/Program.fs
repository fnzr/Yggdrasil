﻿
open System
open System.Net
open NLog
open Yggdrasil.Reporter
open Yggdrasil

let Logger = LogManager.GetCurrentClassLogger()

type TestState = {
    mutable Dispatch: (Command -> unit)
}

let HandleOwnReport state report =
    Logger.Debug "Hello?"
    match report with
    | Dispatcher d -> state.Dispatch <- d
    | ConnectionAccepted _ ->
        Logger.Warn("Dispatching")
        state.Dispatch Command.DoneLoadingMap
        state.Dispatch <| Command.RequestServerTick 1;
    | e -> Logger.Info("Received report {id:A}", e)
    ()

let SupervisorFactory ownId =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<uint32 * AgentReport>) ->
            let state = {
                Dispatch = (fun _ -> Logger.Error("Called dispatch but there's none!"))
            } 
            let rec loop () =  async {
                let! msg = inbox.Receive()
                match msg with
                | (id, report) when id = ownId -> HandleOwnReport state report
                | (otherId, report) ->
                    Logger.Info("Received external {id} Report {report}", otherId, report.ToString())
                | _ -> ()
                return! loop()
            }            
            loop () 
    )

let AgentFactory id = SupervisorFactory id

[<EntryPoint>]
let main argv =
    let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    
    let login = API.CreateServerReporter loginServer AgentFactory   
    login "roboco" "111111"
    
    let line = Console.ReadLine ()
    0 // return an integer exit code
