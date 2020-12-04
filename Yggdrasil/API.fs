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
        mailbox.Error.Add OnMailboxError
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
        
        let args = Console.ReadLine().Split(' ')
        let messageCase = match FindUnionCase CharacterMessageCases args.[0] with
                          | None -> Command
                          | Some(case) ->
                              FSharpValue.MakeUnion(case, [||]) :?> CharacterMessage
        match messageCase with
        | Help -> Handler character mailbox
        | Exit -> ()
        | _ ->
            match FindUnionCase CommandCases args.[1] with
            | None -> match FindUnionCase ReportCases args.[1] with
                      | None -> printfn "Unknown message: %s" args.[1] 
                      | Some (case) -> mailbox.Post <| MakeMessage<Messages.Report> case args.[2..]
            | Some (case) ->
                mailbox.Post (Messages.Command <| MakeMessage<Messages.Command> case args.[2..])
            Handler character mailbox

type MailboxesMessage = | Help | List | Select | Exit | Char of string
let MailboxesMessageCases = FSharpType.GetUnionCases(typeof<MailboxesMessage>)

let ActivateCharacter (mailboxes: Dictionary<string, Mailbox>) name =
    if mailboxes.ContainsKey name
            then CharacterAPI.Handler name mailboxes.[name]
            else printfn "%s not found" name
    
let rec CommandLineHandler (mailboxes: Dictionary<string, Mailbox>) =
    printf ">"
    
    let args = Console.ReadLine().Split(' ')
    let messageCase = match FindUnionCase MailboxesMessageCases args.[0] with
                      | None -> Char args.[0]
                      | Some(case) ->
                          FSharpValue.MakeUnion(case, [||]) :?> MailboxesMessage
    match messageCase with
    | List -> Seq.iter (printfn "%s") mailboxes.Keys; CommandLineHandler mailboxes
    | Help -> CommandLineHandler mailboxes
    | Select -> ActivateCharacter mailboxes args.[1]; CommandLineHandler mailboxes        
    | Char name -> ActivateCharacter mailboxes name; CommandLineHandler mailboxes
    | Exit -> ()
