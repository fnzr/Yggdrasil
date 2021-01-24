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
        State = 0L, 0L
        Initialize = fun node -> {node with State = Connection.Tick() + milliseconds, 0L}
        Tick = fun world instance ->
            let time = Connection.Tick()
            let diff = fst instance.State - time
            if diff > 0L then
                if time > snd instance.State then
                    World.Ping world (int diff+1)
                    Node {instance with State=fst instance.State, time+diff}
                else Node instance
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
                      (Navigation.Maps.GetMapData world.Map) player.Position (x, y) with
                | [] -> Result Failure
                | path ->
                    let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                    world.Request(Types.Request.RequestMove pos)                
                    Result Success
            | None -> Result Success
            
    While WalkingRequired <|
        Sequence [|
            PlayerIs Idle
            RequestWalk
            PlayerIs Walking |> RetryTimeout 2000L
            PlayerIs Walking |> Not |> UntilSuccess 
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
    
let Login =
    Stateless <|
        fun world _ -> world.Login world; Result Success
