module Yggdrasil.Reporter

open System.Collections.Concurrent
open System.IO
open NLog
open Yggdrasil.PacketTypes

let Logger = LogManager.GetCurrentClassLogger()

type AccountId = | Id of uint32

type Command =
    | DoneLoadingMap
    | RequestServerTick of int32
    
type SystemReport =
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
    | SystemReport of SystemReport
    | AgentReport of AgentReport
    
type Mailbox = MailboxProcessor<uint32 * Report>

type AgentMailbox = MailboxProcessor<uint32 * AgentReport>
type SystemMailbox = MailboxProcessor<uint32 * SystemReport>

type ReporterPool = ConcurrentDictionary<uint32, List<AgentMailbox>>

let PublishReport (pool: ReporterPool) (system: SystemMailbox) (source: uint32) (report: Report) =
    match report with
    | SystemReport s -> system.Post <| (source, s)    
    | AgentReport a -> List.iter (fun (m: AgentMailbox) -> m.Post (source, a)) pool.[source]
    
let AddReporter (pool: ReporterPool) (source: uint32) =
    let success = pool.TryAdd(source, List.empty)
    if not success then Logger.Error("Failed adding reporter {id} to report pool", source)
    ()
    
let AddSubscriber (pool: ReporterPool) (source: uint32) (subscriber: AgentMailbox) =
    pool.AddOrUpdate(source, [subscriber], fun id subscribers -> subscriber :: subscribers) |> ignore
    ()
