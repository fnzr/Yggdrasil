module Yggdrasil.Behavior.Trees

open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types
open Yggdrasil.IO

let Walk =
    let DispatchWalk (agent: Agent) =
        match agent.Goals.Position with
        | Some (x, y) ->
            agent.Dispatcher(RequestMove (x, y))
            Success
        | None -> Failure
        
    let WaitWalkAck (agent: Agent) =
        match agent.Destination with
        | Some _ -> Status.Success
        | None -> Status.Running
        
    let StoppedWalking (agent: Agent) =
        match agent.Destination with
        | Some(_) -> Status.Running
        | None -> Status.Success
        
    Sequence[|Action DispatchWalk; Action WaitWalkAck; Action StoppedWalking|]

let Wait milliseconds =
    fun onComplete ->
        let targetTick = Handshake.GetCurrentTick() + milliseconds
        {
            OnComplete = onComplete
            OnTick = fun _ ->
                if Handshake.GetCurrentTick () >= targetTick
                    then Success
                    else Running
        }
        
        
let IsConnected =
    Action <| fun (agent: Agent) ->
                if agent.IsConnected then Success else Failure