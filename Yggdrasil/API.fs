module Yggdrasil.API

open System
open System.Collections.Concurrent
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.PacketTypes
open Yggdrasil.Reporter

type GlobalCommand =
    | List
    | Create
    | Delete
    | Send

let ReportUnionCases = FSharpType.GetUnionCases(typeof<Report>)
let GlobalCommandUnionCases = FSharpType.GetUnionCases(typeof<GlobalCommand>)
let Logger = LogManager.GetCurrentClassLogger()
let ReportPool = ConcurrentDictionary<uint32, List<Mailbox>>()

let Supervisor =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<uint32 * Report>) ->
            let rec loop() =  async {
                let! msg = inbox.Receive()
                match msg with
                | (id, e) -> Logger.Info("[{id}] {event}", id, e)                            
                return! loop()
            }
            loop()
    )

let ConnectAndSupervise (result:  Result<IO.Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        AddReporter ReportPool info.AccountId
        AddSubscriber ReportPool info.AccountId Supervisor
        let publish = PublishReport ReportPool info.AccountId
        let onReceivePacket = IO.Incoming.ZonePacketHandler publish
        IO.Handshake.EnterZone info onReceivePacket
    | Error error -> Logger.Error error

let Login loginServer username password onReadyToEnterZone =
    Async.Start (IO.Handshake.Connect  {
        LoginServer = loginServer
        ReportPool = ReportPool
        Username = username
        Password = password
        CharacterSlot = 0uy
    } onReadyToEnterZone)

let DefaultLogin loginServer username password =
    Login loginServer username password ConnectAndSupervise
    
let ArgumentConverter (value: string) target =
    if target = typeof<Parameter>
    then Enum.Parse(typeof<Parameter>, value)
    else Convert.ChangeType(value, target)
    
let PostMessage (args: string[]) =
    let unionCaseInfo = Array.find
                            (fun (u: UnionCaseInfo) -> u.Name.Equals args.[0])
                            ReportUnionCases    
    
    let convert = fun i (p: PropertyInfo) -> ArgumentConverter args.[1+i] p.PropertyType
    let values = Array.mapi convert <| unionCaseInfo.GetFields()
                 
    let message = FSharpValue.MakeUnion(unionCaseInfo, values) :?> Report
    
    let accountId = Convert.ToUInt32 args.[args.Length-1]
    PublishReport ReportPool accountId message
    Logger.Info("OK")
    
let RunGlobalCommand (args: string) =
    let parts = args.Split(' ')
    let unionCaseInfo = Array.find
                            (fun (u: UnionCaseInfo) -> u.Name.Equals parts.[0])
                            GlobalCommandUnionCases
    let command = FSharpValue.MakeUnion(unionCaseInfo, [||]) :?> GlobalCommand
    
    match command with
    | Create ->
        let id =uint32 ReportPool.Count
        AddReporter ReportPool id 
        AddSubscriber ReportPool id Supervisor
        Logger.Info("Reporter {id} created", id)
    | Send -> PostMessage(parts.[1..])
    | _ -> Logger.Error("Unhandled command")
    
let RunCommand(args) = RunGlobalCommand(args)
    
    
