module Yggdrasil.API

open System
open System.Collections.Concurrent
open System.IO
open System.Net.Sockets
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.IO
open Yggdrasil.Types
open Yggdrasil.Messages
open Yggdrasil.AgentMailbox

type GlobalCommand =
    | List
    | Create
    | Delete
    | Send

LogManager.Setup() |> ignore
let ReportCases = FSharpType.GetUnionCases(typeof<Report>)
let CommandCases = FSharpType.GetUnionCases(typeof<Command>)
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
let PublishReport (mailbox: Mailbox) (report: Report) =  mailbox.Post report
let onAuthenticationResult (office: ConcurrentDictionary<uint32, Mailbox>)
    (behaviorFactory: uint32 -> unit) (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        //behaviorFactory info.AccountId
        let mailbox = MailboxFactory () 
        office.[info.AccountId] <- mailbox
        
        let conn = new TcpClient()
        conn.Connect(info.ZoneServer)
        let stream = conn.GetStream()
        
        mailbox.Post <| Dispatcher (Outgoing.Dispatch stream)        
        
        stream.Write(Handshake.WantToConnect info)
        
        Async.Start <|
        async {
            try
                try                
                    let packetHandler = Incoming.ZonePacketHandler <| PublishReport mailbox
                    return! Array.empty |> IO.Stream.GetReader stream packetHandler
                with
                | :? IOException ->
                    Logger.Error("[{accountId}] MapServer connection closed (timed out?)", info.AccountId)                
                | :? ObjectDisposedException -> ()
            finally
                mailbox.Post <| Disconnected
        }
    | Error error -> Logger.Error error

let Login loginServer onAuthenticationResult username password =
    Async.Start (Handshake.Connect  {
        LoginServer = loginServer
        Username = username
        Password = password
        CharacterSlot = 0uy
    } onAuthenticationResult)
    
let CreateServerOffice loginServer behaviorFactory =
    let office = ConcurrentDictionary<uint32, Mailbox>()
    office, Login loginServer <| onAuthenticationResult office behaviorFactory
    
let ArgumentConverter (value: string) target =
    if target = typeof<Parameter>
    then Enum.Parse(typeof<Parameter>, value)
    else Convert.ChangeType(value, target)
    
let FindMessage name =
    let reportType = Array.tryFind
                            (fun (u: UnionCaseInfo) -> u.Name.Equals name)
                            ReportCases
    match reportType with
    | None -> false, Array.find
                (fun (u: UnionCaseInfo) -> u.Name.Equals name)
                CommandCases
    | Some r -> true, r
    
let PostReport (office: ConcurrentDictionary<uint32, Mailbox>) (args: string[]) =
    let source = Convert.ToUInt32 args.[0]    
    
    let isReport, messageType = FindMessage args.[1]
    
    let convert = fun i (p: PropertyInfo) -> ArgumentConverter args.[2+i] p.PropertyType
    let values = Array.mapi convert <| messageType.GetFields()
    
    let message = FSharpValue.MakeUnion(messageType, values)
    let mailbox = office.[source]
    
    if isReport
        then mailbox.Post (message :?> Report)
        else mailbox.Post (Command (message :?> Command))
    
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