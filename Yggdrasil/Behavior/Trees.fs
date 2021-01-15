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
        let player = game.World.Player
        player.Goals.Position.IsSome &&
        Navigation.Pathfinding.DistanceTo
            player.Position player.Goals.Position.Value > 0        
        
    let DispatchWalk = Action (fun (game: Game) blackboard ->
        let player = game.World.Player
        match player.Goals.Position with
        | Some (x, y) ->
            match Navigation.Pathfinding.FindPath
                      game.World.MapData player.Position (x, y) 0 with
            | [] -> Failure
            | path ->
                let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                player.Dispatch(Types.Command.RequestMove pos)
                blackboard.["MOVE_REQUEST_DISPATCHED"] <- Connection.Tick
                Success
        | None -> Success)
        
    let WaitWalkAck = Action (fun (game: Game) blackboard ->
        match game.World.Player.Status.Action with
        | Event.Idle ->            
            let delay = Connection.Tick - (blackboard.["MOVE_REQUEST_DISPATCHED"] :?> int64)
            if delay > 500L then Status.Failure
            else game.World.PostEvent Event.Ping 500; Status.Running
        | Event.Moving -> Status.Success        
        | _ -> Status.Failure)
        
    let StoppedWalking = Action (fun (game: Game) _ ->
        match game.World.Player.Status.Action with        
        | Event.Moving -> Status.Running
        | Event.Idle -> Status.Success
        | _ -> Status.Failure)
    
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
