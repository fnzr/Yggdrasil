module Yggdrasil.Behavior.Trees

open System
open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types
open Yggdrasil.Agent
open Yggdrasil.Behavior.StateMachine
let Logger = LogManager.GetLogger "BehaviorTree"
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
        
    Sequence [|Action DispatchWalk
               Action WaitWalkAck
               Action StoppedWalking|]

let Wait milliseconds =
    fun onComplete ->
        let targetTick = GetCurrentTick() + milliseconds
        [|{
            OnComplete = onComplete
            OnTick = fun (agent: Agent) ->                
                let diff = targetTick - GetCurrentTick () 
                if diff > 0L then
                    agent.DelayPing <| Convert.ToDouble diff; Running
                else Success
            Aborted = false
        }|]
