module Yggdrasil.Behavior.Trees

open System
open NLog
open Yggdrasil
open Yggdrasil.Game
open Yggdrasil.Behavior.BehaviorTree

let Logger = LogManager.GetLogger "Trees"

[<Literal>]
let MAX_WALK_DISTANCE = 10

let Walk =    
    let WalkingRequired (world: World) =
        let player = world.Player
        player.Goals.Position.IsSome &&
        Navigation.Pathfinding.DistanceTo
            player.Position player.Goals.Position.Value > 0
    
    let DispatchWalk = Action {
        State = ()
        Initialize = id
        Tick = fun world _ -> 
            let player = world.Player
            match player.Goals.Position with
            | Some (x, y) ->
                match Navigation.Pathfinding.FindPath
                      (Navigation.Maps.GetMapData world.Map) player.Position (x, y) 0 with
                | [] -> Output Event.Failure
                | path ->
                    let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                    player.Dispatch(Types.Command.RequestMove pos)                
                    Output Event.Success
            | None -> Output Event.Success
    }
    
    let WaitWalkAck = Action {
            State = 0L
            Initialize = fun node -> {node with State = Connection.Tick()}
            Tick = fun world instance ->
                match world.Player.Unit.Status with
                | Idle ->
                    let delay = Connection.Tick() - instance.State                    
                    if delay > 500L then Result Failure
                    else
                        world.Ping 500
                        Node instance
                | Walking -> Output Event.Success
                | _ -> Output Event.Failure
        }
    
    let StoppedWalking  = Action {
        State = ()
        Initialize = id
        Tick = fun world instance ->
            match world.Player.Unit.Status with        
            | Walking -> Node instance
            | Idle -> Result Success
            | _ -> Result Failure
    }
    
    While WalkingRequired <|
        Sequence [|DispatchWalk; WaitWalkAck; StoppedWalking|]
    (*
    While WalkingRequired
        => (Sequence
            => DispatchWalk
            => WaitWalkAck
            => StoppedWalking)
    *)

let Wait milliseconds =
    Action {
        State = 0L
        Initialize = fun node -> {node with State = Connection.Tick() + milliseconds}
        Tick = fun world instance ->
            let diff = instance.State - Connection.Tick()
            if diff > 0L then
                Logger.Info diff
                world.Ping (int diff)
                Node instance
            else Result Success
                
    }