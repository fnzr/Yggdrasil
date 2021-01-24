module Sunna.Trees

open System
open System.Net
open FSharpPlus.Data
open NLog
open Yggdrasil
open Yggdrasil.Game
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.IO

let Logger = LogManager.GetLogger "Trees"

[<Literal>]
let MAX_WALK_DISTANCE = 10

let Wait milliseconds =
    Action {
        State = 0L
        Initialize = fun node -> {node with State = Connection.Tick() + milliseconds}
        Tick = fun world instance ->
            let diff = instance.State - Connection.Tick()
            if diff > 0L then                             
                if not <| world.PingRequested then World.RequestPing world (int diff)
                Node instance
            else Result Success
                
    }
    
let PlayerIs status =
    Stateless (fun world _ ->
                    Result <| if world.Player.Unit.Status = status
                        then Success
                        else Failure)
    
let WantsToWalk =
    Stateless (fun world _ ->
                    Result <| (match world.Player.Goals.Position with
                                | None -> Failure
                                | Some p -> if p <> world.Player.Position
                                            then Success
                                            else Failure))
    
let RetryTimeout timeout child =
    RetryTimeout Connection.Tick timeout child

let Walk: NodeCreator<World> =
    let WalkingRequired world =
        world.Player.Goals.Position.IsSome &&
            world.Player.Goals.Position.Value <> world.Player.Position
            
    let RequestWalk =
        Stateless <|
        fun world _ -> 
            let player = world.Player
            match player.Goals.Position with
            | Some (x, y) ->
                match Navigation.Pathfinding.FindPath
                      (Navigation.Maps.GetMapData world.Map) player.Position (x, y) 0 with
                | [] -> Result Failure
                | path ->
                    let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                    player.Dispatch(Types.Request.RequestMove pos)                
                    Result Success
            | None -> Result Success
            
    While WalkingRequired <|
        Sequence [|
            PlayerIs Idle
            RequestWalk
            PlayerIs Walking |> RetryTimeout 2000L
            PlayerIs Idle |> UntilSuccess 
        |]
    (*
    While WalkingRequired
        => (Sequence
            => DispatchWalk
            => WaitWalkAck
            => StoppedWalking)
    *)
    
let Disconnected =
    Action {
        State = ()
        Initialize = id
        Tick = fun world _ ->
            Logger.Warn ("Disconnected: {name}", world.Player.Name)
            Result Success
    }
