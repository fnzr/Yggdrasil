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
    let WalkingRequired (game: Game) =
        game.Player.Goals.Position.IsSome &&
        Navigation.Pathfinding.DistanceTo
            game.Player.Position game.Player.Goals.Position.Value > 2        
        
    let DispatchWalk = Action (fun (game: Game) blackboard ->
        match game.Player.Goals.Position with
        | Some (x, y) ->
            match Navigation.Pathfinding.FindPath
                      game.World.MapData game.Player.Position (x, y) 2 with
            | [] -> Failure
            | path ->
                let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                game.Player.Dispatch(Types.Command.RequestMove pos)
                blackboard.["MOVE_REQUEST_DISPATCHED"] <- Connection.Tick
                Success
        | None -> Success)
        
    let WaitWalkAck = Action (fun (game: Game) blackboard ->
        match game.Player.Status.Action with
        | Event.Dead -> Status.Failure
        | Event.Moving -> Status.Success
        | Event.Idle ->            
            let delay = Connection.Tick - (blackboard.["MOVE_REQUEST_DISPATCHED"] :?> int64)
            if delay > 500L then Status.Failure
            else game.World.PostEvent Event.Ping 500; Status.Running)
        
    let StoppedWalking = Action (fun (game: Game) _ ->
        match game.Player.Status.Action with
        | Event.Dead -> Status.Failure
        | Event.Moving -> Status.Running
        | Event.Idle -> Status.Success)
    
    While WalkingRequired
        => (Sequence
            => DispatchWalk
            => WaitWalkAck
            => StoppedWalking)

let Wait milliseconds =
    Factory (
        fun parentName onComplete ->
            let targetTick = Connection.Tick + milliseconds
            {new Node<Game>(parentName, onComplete) with
                override this.Tick game _ =
                    let diff = Convert.ToInt32 (targetTick - Connection.Tick) 
                    if diff > 0 then
                        game.World.PostEvent Event.Ping diff; Running
                    else Success
                override this.Name = "Wait"
            })
