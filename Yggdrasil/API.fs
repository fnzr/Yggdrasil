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
let onAuthenticationResult (mailboxes: Dictionary<string, Mailbox>)
    (behaviorFactory: uint32 -> unit) (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        //behaviorFactory info.AccountId
        let mailbox = MailboxFactory () 
        mailboxes.[info.CharacterName] <- mailbox
        
        let conn = new TcpClient()
        conn.Connect(info.ZoneServer)
        let stream = conn.GetStream()
        
        mailbox.Post <| Dispatcher (Outgoing.Dispatch stream)        
        
        stream.Write(Handshake.WantToConnect info)
        
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
    
module CharacterAPI =
    type CharacterMessage = | Help | Command | Report | Exit
    let CharacterMessageCases = FSharpType.GetUnionCases(typeof<CharacterMessage>)
    let ReportCases = FSharpType.GetUnionCases(typeof<Report>)
    let CommandCases = FSharpType.GetUnionCases(typeof<Command>)    
    let MakeMessage<'T> (case: UnionCaseInfo) (args: string[]) =
        let convert = fun i (p: PropertyInfo) -> ArgumentConverter args.[i] p.PropertyType
        let values = Array.mapi convert <| case.GetFields()    
        FSharpValue.MakeUnion(case, values) :?> 'T
        
    let rec Handler character (mailbox: Mailbox) =
        printf "%s>" character
        
        //0: command
        //1: union case name
        //2?..: args
        let args = Console.ReadLine().Split(' ')
        let messageCase = match FindUnionCase CharacterMessageCases args.[0] with
                          | None -> Help
                          | Some(case) ->
                              FSharpValue.MakeUnion(case, [||]) :?> CharacterMessage
        match messageCase with
        | Command ->
            match FindUnionCase CommandCases args.[1] with
            | None -> printfn "Unknown command: %s" args.[1]
            | Some (case) ->
                mailbox.Post (Messages.Command <| MakeMessage<Messages.Command> case args.[2..])
            Handler character mailbox
        | Report ->
            match FindUnionCase ReportCases args.[1] with
            | None -> printfn "Unknown report: %s" args.[1]
            | Some (case) ->
                mailbox.Post <| MakeMessage<Messages.Report> case args.[2..]
            Handler character mailbox
        | Help -> Handler character mailbox
        | Exit -> ()


type MailboxesMessage = | Help | List | Select | Exit
let MailboxesMessageCases = FSharpType.GetUnionCases(typeof<MailboxesMessage>)
let rec CommandLineHandler (office: Dictionary<string, Mailbox>) =
    printf ">"
    let args = Console.ReadLine().Split(' ')
    let messageCase = match FindUnionCase MailboxesMessageCases args.[0] with
                      | None -> MailboxesMessage.Help
                      | Some(case) ->
                          FSharpValue.MakeUnion(case, [||]) :?> MailboxesMessage
    match messageCase with
    | List -> Seq.iter (printfn "%s") office.Keys; CommandLineHandler office
    | Help -> CommandLineHandler office
    | Select ->
        if office.ContainsKey args.[1]
            then CharacterAPI.Handler args.[1] office.[args.[1]]
            else printfn "%s not found" args.[1]
        CommandLineHandler office
    | Exit -> ()
