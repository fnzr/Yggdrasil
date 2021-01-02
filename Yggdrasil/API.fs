module Yggdrasil.API

open System
open System.Collections.Generic
open System.IO
open System.Net.Sockets
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Agent
open Yggdrasil.Behavior
open Yggdrasil.IO
open Yggdrasil.Types

let Logger = LogManager.GetCurrentClassLogger()

let onAuthenticationResult
    (result:  Result<Handshake.ZoneCredentials, string>) =
    match result with
    | Ok info ->
        let map = info.MapName.Substring(0, info.MapName.Length - 4)
        
        let conn = new TcpClient()
        conn.Connect(info.ZoneServer)
        
        let stream = conn.GetStream()
        let dispatcher = (Outgoing.Dispatch stream)
        
        let agent = Agent(info.CharacterName, map, dispatcher)
        SetupAgentBehavior Machines.DefaultStateMachine Machines.InitialState agent
        stream.Write (Handshake.WantToConnect info)
        
        Async.Start <|
        async {
            try
                try                
                    let packetHandler = Incoming.OnPacketReceived agent
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
    
let CreateServerMailboxes loginServer =
    Handshake.Login loginServer <| onAuthenticationResult
    
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
