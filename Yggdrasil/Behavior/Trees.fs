module Yggdrasil.Behavior.Trees

open System
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types
open Yggdrasil.IO
open Yggdrasil.Agent
let Walk: Factory<Agent> =
    let DispatchWalk (agent: Agent) =
        match agent.Goals.Position with
        | Some (x, y) ->
            agent.Dispatcher(RequestMove (x, y))
            Success
        | None -> Failure
        
    let WaitWalkAck (agent: Agent) =
        match agent.Location.Destination with
        | Some _ -> Status.Success
        | None -> Status.Running
        
    let StoppedWalking (agent: Agent) =
        match agent.Location.Destination with
        | Some _ -> Status.Running
        | None -> Status.Success
        
    Sequence [|Action DispatchWalk [| GoalPositionChanged |]
               Action WaitWalkAck [|DestinationChanged|]
               Action StoppedWalking [|DestinationChanged|] |]

let Wait milliseconds =
    fun onComplete ->
        let targetTick = Handshake.GetCurrentTick() + milliseconds
        [|{
            OnComplete = onComplete
            OnTick = fun (agent: Agent) ->                
                let diff = targetTick - Handshake.GetCurrentTick () 
                if diff > 0L then
                    agent.ScheduleBTTick <| Convert.ToDouble diff; Running
                else Success
            Aborted = false
            Events = [| Ping |]
        }|]
        
let IsConnected =
    Action
        (fun (agent: Agent) -> if agent.IsConnected then Success else Failure)
        [| ConnectionStatusChanged |]