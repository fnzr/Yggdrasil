module Yggdrasil.Behavior.Agent

open NLog
open Yggdrasil.Behavior.FSM

let Logger = LogManager.GetLogger "Agent"

let EventMailbox initialData initialMachineState (inbox: MailboxProcessor<'data -> 'data * 'event list>) =
    let rec loop currentData currentState = async {
        let! update = inbox.Receive()
        let (data, events) = update currentData
        //Logger.Debug ("Updater: {es}", update)
            
        let state =
            events 
            |> List.fold (fun s e -> State.Handle e data s) currentState 
            |> State.Tick data
            |> State.Monitor data
        return! loop data state
        
    }
    loop initialData initialMachineState
    
let SetupAgent initialData initialMachineState =
    let mailbox = MailboxProcessor.Start(EventMailbox initialData initialMachineState)
    mailbox.Error.Add Logger.Error
    mailbox
