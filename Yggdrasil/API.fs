module Yggdrasil.API

open System
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Communication
open Yggdrasil.PacketTypes
open Yggdrasil.Mailbox.Publisher
open Yggdrasil.Mailbox.Agent
open Yggdrasil.YggrasilTypes

type GlobalCommand =
    | List
    | Create
    | Delete
    | Send

let MessageStore = CreatePublisherMailbox()
let mutable Mailboxes = Map.empty
let Logger = LogManager.GetCurrentClassLogger()

let AgentMessageUnionCases = FSharpType.GetUnionCases(typeof<AgentUpdate>)
let GlobalCommandUnionCases = FSharpType.GetUnionCases(typeof<GlobalCommand>)

let Login loginServer username password =
    let id = uint32 Mailboxes.Count
    let mailbox = CreateAgentMailbox id MessageStore
    Mailboxes <- Mailboxes.Add(id, mailbox)
    Async.Start (IO.Handshake.Connect  {
        LoginServer = loginServer
        Mailbox = mailbox
        Username = username
        Password = password
        CharacterSlot = 0uy
    })
    
let ArgumentConverter (value: string) target =
    if target = typeof<Parameter>
    then Enum.Parse(typeof<Parameter>, value)
    else Convert.ChangeType(value, target)
    
let FilterAny (_, _) = true
let Supervisor =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<AgentEvent * Agent>) ->
            let rec loop() =  async {
                let! msg = inbox.Receive()
                match msg with
                | (e, _) -> Logger.Info("{event}", e)                            
                return! loop()
            }
            loop()
    )
MessageStore.Post(Supervise (FilterAny, Supervisor))

let PostMessage (args: string[]) =
    let unionCaseInfo = Array.find
                            (fun (u: UnionCaseInfo) -> u.Name.Equals args.[0])
                            AgentMessageUnionCases    
    
    let values = Array.mapi
                     (fun i (p: PropertyInfo) -> ArgumentConverter args.[1+i] p.PropertyType)
                 <| unionCaseInfo.GetFields()
                 
    let message = FSharpValue.MakeUnion(unionCaseInfo, values) :?> AgentUpdate
    
    let accountId = Convert.ToUInt32 args.[args.Length-1]
    Mailboxes.[accountId].Post message
    Logger.Info("OK")
    
let RunGlobalCommand (args: string) =
    let parts = args.Split(' ')
    let unionCaseInfo = Array.find
                            (fun (u: UnionCaseInfo) -> u.Name.Equals parts.[0])
                            GlobalCommandUnionCases
    let command = FSharpValue.MakeUnion(unionCaseInfo, [||]) :?> GlobalCommand
    
    match command with
    | Create ->
        let id = uint32 Mailboxes.Count
        let mailbox = CreateAgentMailbox id MessageStore        
        Mailboxes <- Mailboxes.Add(id, mailbox) 
        Logger.Info("Agent {id} created", id)
    | Send -> PostMessage(parts.[1..])
    | _ -> Logger.Error("Unhandled command")
    
let RunCommand(args) = RunGlobalCommand(args)
    
    
