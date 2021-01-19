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
    | TargetTick

[<Literal>]
let MAX_WALK_DISTANCE = 10
let Walk =    
    let WalkingRequired (world: World) =
        let player = world.Player
        player.Goals.Position.IsSome &&
        Navigation.Pathfinding.DistanceTo
            player.Position player.Goals.Position.Value > 0
            
    let dispatchWalk (world: World) (blackboard: Map<BlackboardKey, obj>) =
        let player = world.Player
        match player.Goals.Position with
        | Some (x, y) ->
            match Navigation.Pathfinding.FindPath
                      (Navigation.Maps.GetMapData world.Map) player.Position (x, y) 0 with
            | [] -> Result (Failure, blackboard)
            | path ->
                let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                player.Dispatch(Types.Command.RequestMove pos)                
                Result (Success, blackboard.Add (MoveRequested, Connection.Tick()))
        | None -> Result (Success, blackboard)
        
    let DispatchWalk = Action (Node<World,Map<BlackboardKey, obj>>.Create dispatchWalk)
    
    let waitWorldAck (world: World) (bb:  Map<BlackboardKey, obj>) =
        match world.Player.Unit.Status with
        | Idle ->            
            let delay = Connection.Tick() - (bb.[MoveRequested] :?> int64)
            if delay > 500L then Result (Status.Failure, bb)
            else Running <| bb.Add(RequestPing, 500)
        | Walking -> Result (Success, bb)
        | _ -> Result (Failure, bb)
    
    let WaitWalkAck = Action <| Node<World, Map<BlackboardKey, obj>>.Create waitWorldAck
    
    let stoppedWalking (world: World) bb =
        match world.Player.Unit.Status with        
        | Walking -> Running bb
        | Idle -> Result (Success, bb)
        | _ -> Result (Failure, bb)
        
    let StoppedWalking = Action <| Node<_,_>.Create stoppedWalking
    //let Root  (_:World, _:Map<BlackboardKey, obj>, status): NodeResult<World, Map<BlackboardKey, obj>> = End status
    While WalkingRequired
        (Sequence [|DispatchWalk; WaitWalkAck; StoppedWalking|])
    //While WalkingRequired |>
    (*
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
    *)
let Wait<'Data> milliseconds =
    Action {
        Finalize = id
        Initialize = fun (bb: Map<BlackboardKey, obj>) ->
            let target = Connection.Tick() + milliseconds
            bb.Add(TargetTick, target).Add(RequestPing, milliseconds).Add(PingRequested, true)
        Tick = fun (_: 'Data) bb ->
                let diff = Connection.Tick() - (Convert.ToInt64 bb.[TargetTick])
                if diff > 0L then
                    Running <|
                        if bb.ContainsKey PingRequested then bb
                        else bb.Add(RequestPing, diff).Add(PingRequested, true)
                else Result (Success, bb)
                     
        }