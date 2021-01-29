module Yggdrasil.Behavior.EventHandler

open NLog
open Yggdrasil.Game
let Logger = LogManager.GetLogger "Event"

let EventMailbox initialData initialMachineState (inbox: MailboxProcessor<GameUpdate>) =
    let rec loop currentData currentState = async {
        let! update = inbox.Receive()
        //Logger.Info update
        
        match update with
        | PlayerId id ->
            printfn "huh"
            Propagators.PlayerId.OnNext id
        | _ -> ()
            
        //let (newData, newState) =
          //  (data, currentState)
            //|> State.MoveState
            //|> State.Tick
        return! loop currentData currentState
    }
    loop initialData initialMachineState
    
let SetupAgent initialData initialMachineState =
    let mailbox = MailboxProcessor.Start(EventMailbox initialData initialMachineState)
    mailbox.Error.Add Logger.Error
    mailbox
