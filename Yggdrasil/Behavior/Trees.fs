module Yggdrasil.Behavior.Trees

open System
open NLog
open Yggdrasil
open Yggdrasil.Game
open Yggdrasil.Behavior.BehaviorTree

let Logger = LogManager.GetLogger "Trees"

type BlackboardKey =
    | RequestPing
    | MoveRequested
    | PingRequested
    interface MapKey

[<Literal>]
let MAX_WALK_DISTANCE = 10
let Walk =    
    let WalkingRequired (world: World) =
        let player = world.Player
        player.Goals.Position.IsSome &&
        Navigation.Pathfinding.DistanceTo
            player.Position player.Goals.Position.Value > 0        
        
    let DispatchWalk = Action (fun (world: World) blackboard ->
        let player = world.Player
        match player.Goals.Position with
        | Some (x, y) ->
            match Navigation.Pathfinding.FindPath
                      (Navigation.Maps.GetMapData world.Map) player.Position (x, y) 0 with
            | [] -> Failure, blackboard
            | path ->
                let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                player.Dispatch(Types.Command.RequestMove pos)                
                Success, blackboard.Add (MoveRequested, Connection.Tick())
        | None -> Success, blackboard)
        
    let WaitWalkAck = Action (fun (world: World) blackboard ->
        match world.Player.Unit.Status with
        | Idle ->            
            let delay = Connection.Tick() - (blackboard.[MoveRequested] :?> int64)
            if delay > 500L then Status.Failure, blackboard
            else Status.Running, blackboard.Add(RequestPing, 500)
        | Walking -> Status.Success, blackboard
        | _ -> Status.Failure, blackboard)
        
    let StoppedWalking = Action (fun (world: World) bb ->
        (match world.Player.Unit.Status with        
        | Walking -> Status.Running
        | Idle -> Status.Success
        | _ -> Status.Failure), bb)
    
    While WalkingRequired
        => (Sequence
            => DispatchWalk
            => WaitWalkAck
            => StoppedWalking)

let Wait milliseconds =
    Factory (
        fun parentName onComplete ->
            let targetTick = Connection.Tick() + milliseconds
            {new Node<World>(parentName, onComplete) with
                override this.Tick world bb =
                    let diff = Convert.ToInt32 (targetTick - Connection.Tick())
                    if diff > 0 then                        
                        Running,
                        if bb.ContainsKey PingRequested then bb
                        else bb.Add(RequestPing, diff).Add(PingRequested, true)
                    else Success, bb
                override this.Name = "Wait"
            })
