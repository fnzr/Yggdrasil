module Yggdrasil.Behavior.Agent

open NLog
open Yggdrasil.Behavior.FSM

let Logger = LogManager.GetLogger "Agent"

let EventMailbox initialData initialMachineState (inbox: MailboxProcessor<'data -> 'data>) =
    let rec loop currentData currentState = async {
        let! update = inbox.Receive()
        let data = update currentData
        //Logger.Info ("{es}", update)
            
        let (newData, newState) =
            (data, currentState)
            |> State.MoveState
            |> State.Tick
        //let newState =  data midState
        return! loop newData newState
    }
    loop initialData initialMachineState
    
let SetupAgent initialData initialMachineState =
    let mailbox = MailboxProcessor.Start(EventMailbox initialData initialMachineState)
    mailbox.Error.Add Logger.Error
    mailbox
