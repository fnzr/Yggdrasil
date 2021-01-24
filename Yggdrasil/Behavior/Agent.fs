module Yggdrasil.Behavior.Agent

open NLog
open Yggdrasil.Behavior.FSM

let Logger = LogManager.GetLogger "Agent"

let EventMailbox initialData initialMachineState (inbox: MailboxProcessor<'data -> 'data>) =
    let rec loop currentData currentState = async {
        let! update = inbox.Receive()
        let data = update currentData
        //Logger.Debug ("Updater: {es}", update)
            
        let state =
            currentState
            |> State.MoveState data
            |> State.Tick data
        return! loop data state
        
    }
    loop initialData initialMachineState
    
let SetupAgent initialData initialMachineState =
    let mailbox = MailboxProcessor.Start(EventMailbox initialData initialMachineState)
    mailbox.Error.Add Logger.Error
    mailbox
