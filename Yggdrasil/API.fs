module Yggdrasil.API

open System
open System.Collections.Concurrent
open System.IO
open System.Net.Sockets
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.IO
open Yggdrasil.PacketTypes
open Yggdrasil.Reporter

type GlobalCommand =
    | List
    | Create
    | Delete
    | Send

let ReportUnionCases = FSharpType.GetUnionCases(typeof<Report>)
let AutomatonReportCases = FSharpType.GetUnionCases(typeof<AutomatonReport>)
let AgentReportCases = FSharpType.GetUnionCases(typeof<AgentReport>)
let GlobalCommandUnionCases = FSharpType.GetUnionCases(typeof<GlobalCommand>)
let Logger = LogManager.GetCurrentClassLogger()

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

let onAuthenticationResult reporter
    (agentFactory: uint32 -> AgentMailbox) (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        let agent = agentFactory info.AccountId
        reporter.AddSubscriber info.AccountId agent
        
        let conn = new TcpClient()
        conn.Connect(info.ZoneServer)
        let stream = conn.GetStream()
        
        agent.Post <| (info.AccountId, Dispatcher <| Outgoing.Dispatch stream)        
        
        Utils.Write stream <| Handshake.WantToConnect info
        
        Async.Start <|
        async {
            try
                let packetHandler = Incoming.ZonePacketHandler <| reporter.PublishReport info.AccountId
                return! Array.empty |> IO.Stream.GetReader stream packetHandler
            with
            | :? IOException -> Logger.Error("[{accountId}] MapServer connection closed (timed out?)", info.AccountId)
            | :? ObjectDisposedException -> ()
        }
    | Error error -> Logger.Error error

let Login loginServer onAuthenticationResult username password =
    Async.Start (Handshake.Connect  {
        LoginServer = loginServer
        Username = username
        Password = password
        CharacterSlot = 0uy
    } onAuthenticationResult)
    
let CreateServerReporter loginServer agentFactory =
    let reporter = CreateReporter()
    reporter, Login loginServer <| onAuthenticationResult reporter agentFactory
    
let ArgumentConverter (value: string) target =
    if target = typeof<Parameter>
    then Enum.Parse(typeof<Parameter>, value)
    else Convert.ChangeType(value, target)

let PostReport reporter (args: string[]) =
    let source = Convert.ToUInt32 args.[0]
    
    let unionCases =
        if source = 0u then AutomatonReportCases
        else AgentReportCases
        
    let reportType = Array.find
                            (fun (u: UnionCaseInfo) -> u.Name.Equals args.[1])
                            unionCases
    
    let convert = fun i (p: PropertyInfo) -> ArgumentConverter args.[2+i] p.PropertyType
    let values = Array.mapi convert <| reportType.GetFields()
    
    let report =
        if source = 0u then AutomatonReport (FSharpValue.MakeUnion(reportType, values) :?> AutomatonReport)
        else AgentReport (FSharpValue.MakeUnion(reportType, values) :?> AgentReport)
    reporter.PublishReport source report
    
(* 
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
*)