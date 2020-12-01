module Yggdrasil.Reporter

open System.Collections.Concurrent
open NLog
open Yggdrasil.PacketTypes

let Logger = LogManager.GetCurrentClassLogger()

type AccountId = | Id of uint32

type Command =
    | DoneLoadingMap
    | RequestServerTick of int32
    
type AutomatonReport =
    | UnitSpawn of Unit
    
type AgentReport =
    | Dispatcher of (Command -> unit)
    | Name of string
    | AccountId of uint32
    | ConnectionAccepted of StartData
    | StatusU32 of Parameter * uint32
    | StatusI32 of Parameter * int
    | StatusU16 of Parameter * uint16
    | StatusI16 of Parameter * int16
    | StatusPair of Parameter * uint16 * int16
    | Status64 of Parameter * int64
    | WeightSoftCap of int
    | Print

type Report =
    | AutomatonReport of AutomatonReport
    | AgentReport of AgentReport
    
(*
let PublishReport (pool: ReporterPool) (system: SystemMailbox) (source: uint32) (report: Report) =
    match report with
    | SystemReport s -> system.Post <| (source, s)    
    | AgentReport a -> List.iter (fun (m: AgentMailbox) -> m.Post (source, a)) pool.[source]
    
let CreatePublisher (pool: ReporterPool) =
    PublishReport pool <| System.CreateSystem pool
    
let RegisterReporter (pool: ReporterPool) (id: uint32) =
    let success = pool.TryAdd(id, List.empty)
    if not success then Logger.Error("Failed adding reporter {id} to report pool", id)
    ()
    
let AddSubscriber (pool: ReporterPool) (source: uint32) (subscriber: AgentMailbox) =
    pool.AddOrUpdate(source, [subscriber], fun id subscribers -> subscriber :: subscribers) |> ignore
    ()
*)
type AgentMailbox = MailboxProcessor<uint32 * AgentReport>
type AutomatonMailbox = MailboxProcessor<uint32 * AutomatonReport>

type SubscriberPool = ConcurrentDictionary<uint32, List<AgentMailbox>>
type Subscriber = MailboxProcessor<uint32 * AgentReport>

type Reporter =
    {
        PublishReport: uint32 -> Report -> unit
        AddSubscriber: uint32 -> Subscriber -> unit
        RemoveSubscriber: uint32 -> Subscriber -> unit
    }

let private PublishToAutomaton (automaton: AutomatonMailbox) (source: uint32) (report: AutomatonReport) =
    automaton.Post <| (source, report)
let private PublishToSubscribers (pool: SubscriberPool) (source: uint32) (report: AgentReport) =    
    if pool.ContainsKey(source) then
        List.iter (fun (m: AgentMailbox) -> m.Post (source, report)) pool.[source]
let private AddSubscriber (pool: SubscriberPool) (source: uint32) (subscriber: Subscriber) =
    pool.AddOrUpdate(source, [subscriber], fun id subscribers -> subscriber :: subscribers) |> ignore
let private RemoveSubscriber (pool: SubscriberPool) (source: uint32) (subscriber: Subscriber) =
    if pool.ContainsKey(source) then
        let list = pool.[source]
        pool.[source] <- List.except [subscriber] list
let private PublishReport (automaton: AutomatonMailbox) (pool: SubscriberPool) (source: uint32) (report: Report) =
    match report with
    | AutomatonReport s -> PublishToAutomaton automaton source s    
    | AgentReport a -> PublishToSubscribers pool source a
let private CreateAutomaton (pool: SubscriberPool) =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<uint32 * AutomatonReport>) ->
            let rec loop () =  async {
                let! msg = inbox.Receive()
                match msg with
                | (id, report) -> Logger.Info("Received system info from {id}: {report}", id, report.ToString())         
                return! loop()
            }            
            loop () 
    )
    
let CreateReporter () =
    let pool = SubscriberPool()
    let automaton = CreateAutomaton pool    
    {
        PublishReport = PublishReport automaton pool
        AddSubscriber = AddSubscriber pool
        RemoveSubscriber = RemoveSubscriber pool
    }
