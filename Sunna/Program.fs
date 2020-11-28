// Learn more about F# at http://fsharp.org

open System
open System.Collections.Concurrent
open System.Net
open Yggdrasil.Reporter
open Yggdrasil.IO
let ReportPool = ConcurrentDictionary<uint32, List<Mailbox>>()

let Supervisor =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<uint32 * Report>) ->
            let rec loop() =  async {
                let! msg = inbox.Receive()
                match msg with
                | (_, WeightSoftCap d) -> Logger.Info(d)
                | _ -> ()
                return! loop()
            }
            loop()
    )

let onReadyToEnterZone (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        AddReporter ReportPool info.AccountId
        AddSubscriber ReportPool info.AccountId Supervisor
        let publish = PublishReport ReportPool info.AccountId
        let onReceivePacket = Incoming.ZonePacketHandler publish
        Handshake.EnterZone info onReceivePacket
    | Error error -> Logger.Error error

[<EntryPoint>]
let main argv =
    let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    Yggdrasil.API.Login loginServer "roboco" "111111" onReadyToEnterZone
    
    let line = Console.ReadLine ()
    0 // return an integer exit code
