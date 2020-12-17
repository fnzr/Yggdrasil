module Yggdrasil.API

open System
open System.Collections.Generic
open System.IO
open System.Net.Sockets
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.BehaviorTree
open Yggdrasil.IO
open Yggdrasil.Types

let Logger = LogManager.GetCurrentClassLogger()

let Agents = Dictionary<string, State>()
let mutable ActiveAgent = None

let onAuthenticationResult (states: Dictionary<string, State>)
    (behaviorFactory: string -> unit) (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        let map = info.MapName.Substring(0, info.MapName.Length - 4)
        let behaviorTree = behaviorFactory info.CharacterName        
        
        let conn = new TcpClient()
        conn.Connect(info.ZoneServer)
        let stream = conn.GetStream()
        let dispatcher = (Outgoing.Dispatch stream)
        
        let mailbox = Mailbox.MailboxFactory info.CharacterName map dispatcher
        let state = State.Create dispatcher map mailbox
        states.[info.CharacterName] <- state
        stream.Write(Handshake.WantToConnect info)
        
        ActiveAgent <- Some(state)
        
        Async.Start <|
        async {
            try
                try                
                    let packetHandler = Incoming.OnPacketReceived state
                    return! Array.empty |> IO.Stream.GetReader stream packetHandler
                with
                //| :? IOException ->
                  //  Logger.Error("[{accountId}] MapServer connection closed (timed out?)", info.AccountId)                
                | :? ObjectDisposedException -> ()
            finally
                //mailbox.Post <| Disconnected
                ()
        }
    | Error error -> Logger.Error error
    
let CreateServerMailboxes loginServer behaviorFactory =
    Agents, Handshake.Login loginServer <| onAuthenticationResult Agents behaviorFactory
    
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
let rec CommandLineHandler (agents: Dictionary<string, State>) =
    printf ">"
    let CommandCases = FSharpType.GetUnionCases(typeof<Command>)
    
    let args = Console.ReadLine().Split(' ')
    if args.[0].Equals "print" then printfn "%A" ActiveAgent
    else
        let agent = match ActiveAgent with
                        | Some(m) -> m
                        | None -> raise <| InvalidOperationException()
        match FindUnionCase CommandCases args.[0] with
            | Some (com) -> agent.Dispatch <| MakeMessage<Command> com args.[1..]
            | None -> printfn "Message not found"
    CommandLineHandler agents
