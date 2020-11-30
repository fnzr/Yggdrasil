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

type Report =
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
    | UnitSpawn of Unit
    | Print
    
type Mailbox = MailboxProcessor<uint32 * Report>

type ReporterPool = ConcurrentDictionary<uint32, List<Mailbox>>

let PublishReport (pool: ReporterPool) (source: uint32) (report: Report) =
    List.iter (fun (m: Mailbox) -> m.Post(source, report)) pool.[source]
    
let AddReporter (pool: ReporterPool) (source: uint32) =
    let success = pool.TryAdd(source, List.empty)
    if not success then Logger.Error("Failed adding reporter {id} to report pool", source)
    ()
    
let AddSubscriber (pool: ReporterPool) (source: uint32) (subscriber: Mailbox) =
    pool.AddOrUpdate(source, [subscriber], fun id subscribers -> subscriber :: subscribers) |> ignore
    ()
    
let SystemPublish (system: Mailbox) (source: uint32) (report: Report) =
    system.Post <| (source, report)
    