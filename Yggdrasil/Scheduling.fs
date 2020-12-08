module Yggdrasil.Scheduling

open System
open System.Diagnostics
open Priority_Queue

type SchedulerMailbox = MailboxProcessor<int64 * Messages.Report>

type TimedEventNode(time, event) =
    inherit FastPriorityQueueNode()
    member public this.Event = event
    member public this.Time = time
let Stopwatch = Stopwatch()
Stopwatch.Start()
let GetCurrentTick() = Stopwatch.ElapsedMilliseconds

let OnTick (mailbox: Messages.Mailbox) queueLock (eventQueue: FastPriorityQueue<TimedEventNode>) _ =
    let tick = GetCurrentTick()
    let rec dequeue () =        
        if eventQueue.Count > 0 &&
           eventQueue.First.Time < tick
           then
                mailbox.Post <| eventQueue.Dequeue().Event 
                dequeue()
    lock queueLock dequeue
    
let ScheduledTimedCallback (scheduler: SchedulerMailbox) time event = scheduler.Post <| (time, event)
    
let SchedulerFactory mailbox =
    MailboxProcessor.Start(
        fun (inbox) ->
            let timedEventsQueue = FastPriorityQueue<TimedEventNode>(32)
            let queueLock = obj()
            let timer = new System.Timers.Timer(50.0)
            timer.Elapsed.Add(OnTick mailbox queueLock timedEventsQueue)
            timer.Enabled <- true
            timer.AutoReset <- true
            let rec loop queueLock (queue: FastPriorityQueue<TimedEventNode>) tmr =  async {
                let! (tick, cb) = inbox.Receive()
                lock queueLock (fun () -> queue.Enqueue(TimedEventNode(tick, cb), Convert.ToSingle(tick)))
                return! loop queueLock queue tmr
            }
            loop queueLock timedEventsQueue timer
    )