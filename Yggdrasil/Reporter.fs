module Yggdrasil.Reporter

open System.Collections.Concurrent
open NLog
open Yggdrasil.PacketTypes

let Logger = LogManager.GetCurrentClassLogger()

type AccountId = | Id of uint32

type Report =
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
    
type Mailbox = MailboxProcessor<uint32 * Report>

type ReportPool = ConcurrentDictionary<uint32, List<Mailbox>> 

let PublishReport (pool: ReportPool) (source: uint32) (report: Report) =
    List.iter (fun (m: Mailbox) -> m.Post(source, report)) pool.[source]
    
let AddReporter (pool: ReportPool) (source: uint32) =
    let success = pool.TryAdd(source, List.empty)
    if not success then Logger.Error("Failed adding reporter {id} to report pool", source)
    ()
    
let AddSubscriber (pool: ReportPool) (source: uint32) (subscriber: Mailbox) =
    pool.AddOrUpdate(source, [subscriber], fun id subscribers -> subscriber :: subscribers) |> ignore
    ()