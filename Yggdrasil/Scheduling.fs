module Yggdrasil.Scheduling

let OnTick sender e =
    ()
    
let ScheduleEvent (scheduler: MailboxProcessor<int * unit -> unit>) delay fn =
    scheduler.Post <| delay, fn
    
let DispatcherFactory () =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<int * unit->unit>) ->            
            let rec loop () =  async {
                let! msg = inbox.Receive()
                return! loop()
            }
            loop ()
    )