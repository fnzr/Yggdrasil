module Yggdrasil.Behavior.Trees

open System
open NLog
open Yggdrasil
open Yggdrasil.Agent
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types

let Logger = LogManager.GetLogger "Trees"

[<Literal>]
let MAX_WALK_DISTANCE = 10
let Walk =    
    let WalkingRequired (agent: Agent) =
        match agent.Goals.Position with
        | Some v -> v <> agent.Location.Position
        | None -> false
        
    let DispatchWalk = Action (fun (agent: Agent) ->
        match agent.Goals.Position with
        | Some (x, y) ->
            match agent.Location.PathTo (x, y) 2 with
            | [] -> Failure
            | path ->
                let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                agent.Dispatcher(RequestMove pos)
                agent.BehaviorData.["MOVE_REQUEST_DISPATCHED"] <- Agent.Tick
                Success
        | None -> Success)
        
    let WaitWalkAck = Action (fun (agent: Agent) ->
        match agent.Location.Destination with
        | Some _ -> Status.Success
        | None ->
            let delay = Agent.Tick - (agent.BehaviorData.["MOVE_REQUEST_DISPATCHED"] :?> int64)
            if delay > 500L then Status.Failure else Status.Running)
        
    let StoppedWalking = Action (fun (agent: Agent) ->
        match agent.Location.Destination with
        | Some _ -> Status.Running
        | None ->
            if Option.isSome agent.Goals.Position &&
               agent.Location.DistanceTo agent.Goals.Position.Value <= 2 then
                agent.Goals.Position <- None
            else agent.DelayPing 100.0
            Status.Success)
    
    While WalkingRequired
        => (Sequence
            => DispatchWalk
            => WaitWalkAck
            => StoppedWalking)

let Wait milliseconds =
    Factory (
        fun parentName onComplete ->
            let targetTick = Agent.Tick + milliseconds
            [|{new Node<Agent>(parentName, onComplete) with
                member this.OnTick agent =
                    let diff = targetTick - Agent.Tick 
                    if diff > 0L then
                        agent.DelayPing <| Convert.ToDouble diff; Running
                    else Success
                member this.Name = "Wait"
            }|])
