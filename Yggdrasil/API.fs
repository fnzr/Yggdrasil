module Yggdrasil.API

open System
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Handshake
open Yggdrasil.PacketTypes
open Yggdrasil.Publisher

type GlobalCommand =
    | List
    | Create
    | Delete
    | Send

let MessageStore = CreatePublisher()
let mutable Mailboxes = Map.empty
let Logger = LogManager.GetCurrentClassLogger()

let AgentMessageUnionCases = FSharpType.GetUnionCases(typeof<PacketTypes.Message>)
let GlobalCommandUnionCases = FSharpType.GetUnionCases(typeof<GlobalCommand>)

module Handshake =    
    let OnCharacterSelected messageStore info characterName =
        let mailbox = ZoneService.Connect info messageStore
        Mailboxes <- Mailboxes.Add(info.AccountId, mailbox)
        Logger.Info("Character {characterName}[{accountId}] is ready.", characterName, info.AccountId)        
    
    let OnLoginSuccess messageStore (ipEndPoint, credentials) =
        CharacterService.SelectCharacter ipEndPoint credentials 0uy <| OnCharacterSelected messageStore
        
let Login loginServer username password =
    LoginService.Authenticate loginServer username password <| Handshake.OnLoginSuccess MessageStore

let ArgumentConverter (value: string) target =
    if target = typeof<StatusCode>
    then Enum.Parse(typeof<StatusCode>, value)
    else Convert.ChangeType(value, target)
    
let FilterAny (event, agent) = true
let Supervisor =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<YggrasilTypes.Event * YggrasilTypes.Agent>) ->
            let rec loop() =  async {
                let! msg = inbox.Receive()
                match msg with
                | e -> Logger.Info("{event}", e)                            
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
                 
    let message = FSharpValue.MakeUnion(unionCaseInfo, values) :?> Message
    
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
        let mailbox = Agent.CreateAgentMailbox id MessageStore        
        Mailboxes <- Mailboxes.Add(id, mailbox) 
        Logger.Info("Agent {id} created", id)
    | Send -> PostMessage(parts.[1..])
    | _ -> Logger.Error("Unhandled command")
    
let RunCommand(args) = RunGlobalCommand(args)
    
    
