module Yggdrasil.System

open NLog
open Yggdrasil.Reporter

let Logger = LogManager.GetCurrentClassLogger()

let CreateSystem (pool: ReporterPool) =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<uint32 * SystemReport>) ->
            let rec loop () =  async {
                let! msg = inbox.Receive()
                match msg with
                | (id, report) -> Logger.Info("Received system info from {id}: {report}", id, report.ToString())         
                return! loop()
            }            
            loop () 
    )