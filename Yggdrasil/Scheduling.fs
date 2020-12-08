module Yggdrasil.Scheduling

open System
open System.Diagnostics
open Priority_Queue

type TimedEventNode(time, callback) =
    inherit FastPriorityQueueNode()
    member public this.Callback = callback
    member public this.Time = time

let QueueLock = obj()
let Stopwatch = Stopwatch()
Stopwatch.Start()
let GetCurrentTick() = Convert.ToUInt32 (Stopwatch.ElapsedMilliseconds)

let TimedEventsQueue = FastPriorityQueue<TimedEventNode>(512)

let OnTick _ =
    let tick = GetCurrentTick()
    let rec dequeue () =        
        if TimedEventsQueue.Count > 0 &&
           TimedEventsQueue.First.Time < tick
           then
                let task = TimedEventsQueue.Dequeue()
                Async.Start <| async { task.Callback() }
                dequeue()
    lock QueueLock dequeue
    
let DispatcherFactory () =
    MailboxProcessor.Start(
        fun (inbox) ->
            let timer = new System.Timers.Timer(50.0)
            timer.Elapsed.Add(OnTick)
            timer.Enabled <- true
            timer.AutoReset <- true
            let rec loop tmr =  async {
                let! (tick: uint32, cb) = inbox.Receive()
                lock QueueLock (fun () -> TimedEventsQueue.Enqueue(TimedEventNode(tick, cb), Convert.ToSingle(tick)))
                return! loop tmr
            }
            loop timer
    )