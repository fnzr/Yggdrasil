module Yggdrasil.API

open System
open System.Collections.Generic
open System.IO
open System.Net.Sockets
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.IO
open Yggdrasil.Types
open Yggdrasil.Messages
open Yggdrasil.AgentMailbox

let Logger = LogManager.GetCurrentClassLogger()
let mutable ActiveMailbox: Mailbox option = None

let onAuthenticationResult (mailboxes: Dictionary<string, Mailbox>)
    (behaviorFactory: uint32 -> unit) (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        //behaviorFactory info.AccountId
        let mailbox = MailboxFactory ()
        mailboxes.[info.CharacterName] <- mailbox
        mailbox.Error.Add OnMailboxError
        
        mailbox.Post <| Mailbox(mailbox)
        let scheduler = Scheduling.ScheduledTimedCallback <| Scheduling.SchedulerFactory(mailbox)
        mailbox.Post <| Scheduler(scheduler)
        let conn = new TcpClient()
        conn.Connect(info.ZoneServer)
        let stream = conn.GetStream()
        mailbox.Post <| Dispatcher (Outgoing.Dispatch stream)        
        stream.Write(Handshake.WantToConnect info)
        
        ActiveMailbox <- Some(mailbox)
        
        Async.Start <|
        async {
            try
                try                
                    let packetHandler = Incoming.ZonePacketHandler <| fun report -> mailbox.Post report
                    return! Array.empty |> IO.Stream.GetReader stream packetHandler
                with
                | :? IOException ->
                    Logger.Error("[{accountId}] MapServer connection closed (timed out?)", info.AccountId)                
                | :? ObjectDisposedException -> ()
            finally
                mailbox.Post <| Disconnected
        }
    | Error error -> Logger.Error error
    
let CreateServerMailboxes loginServer behaviorFactory =
    let mailboxes = Dictionary<string, Mailbox>()
    mailboxes, Handshake.Login loginServer <| onAuthenticationResult mailboxes behaviorFactory
    
let ArgumentConverter (value: string) target =
    if target = typeof<Parameter>
    then Enum.Parse(typeof<Parameter>, value)
    else Convert.ChangeType(value, target)
    
let FindUnionCase cases name =
    Array.tryFind
        (fun (u: UnionCaseInfo) ->
                String.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase))
        cases

type MailboxesMessage = | Help | List | Select | Exit | Char of string
let MailboxesMessageCases = FSharpType.GetUnionCases(typeof<MailboxesMessage>)
    
let MakeMessage<'T> (case: UnionCaseInfo) (args: string[]) =
        let convert = fun i (p: PropertyInfo) -> ArgumentConverter args.[i] p.PropertyType
        let values = Array.mapi convert <| case.GetFields()    
        FSharpValue.MakeUnion(case, values) :?> 'T
let rec CommandLineHandler (mailboxes: Dictionary<string, Mailbox>) =
    printf ">"
    
    let ReportCases = FSharpType.GetUnionCases(typeof<Report>)
    let CommandCases = FSharpType.GetUnionCases(typeof<Command>)
    
    let args = Console.ReadLine().Split(' ')
    let mailbox = match ActiveMailbox with
                    | Some(m) -> m
                    | None -> raise <| InvalidOperationException()
    match FindUnionCase ReportCases args.[0] with
        | None -> match FindUnionCase CommandCases args.[0] with
                  | Some (com) -> mailbox.Post (Command <| MakeMessage<Messages.Command> com args.[1..])
                  | None -> printfn "Message not found"
        | Some(rep) -> mailbox.Post (MakeMessage<Report> rep args.[1..])
    CommandLineHandler mailboxes
                          (*
    match messageCase with
    | List -> Seq.iter (printfn "%s") mailboxes.Keys; CommandLineHandler mailboxes
    | Help -> CommandLineHandler mailboxes
    | Select -> ActivateCharacter mailboxes args.[1]; CommandLineHandler mailboxes        
    | Char name -> ActivateCharacter mailboxes name; CommandLineHandler mailboxes
    | Exit -> ()
    *)
