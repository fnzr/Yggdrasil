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
        Tick = fun game instance ->
            let time = Connection.Tick()
            let diff = fst instance.State - time
            if diff > 0L then
                if time > snd instance.State then
                    Game.Ping game (int diff+1)
                    Node {instance with State=fst instance.State, time+diff}
                else Node instance
            else Result Success
                
    }
    
let PlayerIs status =
    Stateless (fun (game: Game) _ ->
                    Result <| if game.Player.Action =~ status
                        then Success
                        else Failure)
    
let WantsToWalk =
    Stateless (fun game _ ->
                    Result <| (match game.Goals.Position with
                                | None -> Failure
                                | Some p -> if p <> game.Player.Position
                                            then Success
                                            else Failure))
    
let RetryTimeout timeout child =
    RetryTimeout Connection.Tick timeout child

let Walk: NodeCreator<Game> =
    let WalkingRequired game =
        game.Goals.Position.IsSome &&
            game.Goals.Position.Value <> game.Player.Position
            
    let RequestWalk =
        Stateless <|
        fun (game: Game) _ -> 
            let player = game.Player
            match game.Goals.Position with
            | Some (x, y) ->
                match Navigation.Pathfinding.FindPath
                      (Navigation.Maps.GetMapData game.Map) player.Position (x, y) with
                | [] -> Result Failure
                | path ->
                    let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                    game.Request(Types.Request.RequestMove pos)                
                    Result Success
            | None -> Result Success
            
    While WalkingRequired <|
        Sequence [|
            Condition Game.PlayerIsIdle
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
        Tick = fun (game: Game) _ ->
            Logger.Warn ("Disconnected: {name}", game.Player.Name)
            Result Success
    }
    
let Login =
    Stateless <|
        fun game _ -> game.Login game; Result Success
