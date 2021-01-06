module Yggdrasil.Behavior.Trees

open System
open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types
open Yggdrasil.Agent

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
            match agent.Location.PathTo (x, y) with
            | [] -> Failure
            | path ->
                let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                agent.Dispatcher(RequestMove pos)
                Success
        | None -> Failure)
        
    let WaitWalkAck = Action (fun (agent: Agent) ->
        match agent.Location.Destination with
        | Some _ -> Status.Success
        | None -> Status.Running)
        
    let StoppedWalking = Action (fun (agent: Agent) ->
        match agent.Location.Destination with
        | Some _ -> Status.Running
        | None ->
            if agent.Goals.Position = Some(agent.Location.Position) then
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
            let targetTick = GetCurrentTick() + milliseconds
            [|{new Node<Agent>(parentName, onComplete) with
                member this.OnTick agent =
                    let diff = targetTick - GetCurrentTick () 
                    if diff > 0L then
                        agent.DelayPing <| Convert.ToDouble diff; Running
                    else Success
                member this.Name = "Wait"
            }|])
