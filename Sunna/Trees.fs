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

let Wait milliseconds parent data =
    let rec _wait startTime nextPing game =
        let time = Connection.Tick()
        let diff = time - (startTime + milliseconds)
        if diff >= 0L then parent Success game
        else
            if time >= nextPing then
                Game.Ping game (int diff)
                Next <| _wait startTime (nextPing+diff)
            else Next <| _wait startTime nextPing
    _wait (Connection.Tick()) 0L data    
let PlayerIs status = Condition (fun (g: Game) -> g.Player.Action = status)
//((Game * Status -> 'a) -> Game -> 'b -> 'a)
let WantsToWalk: Node<Game> =
    Condition <|
        fun g ->
        match g.Goals.Position with
        | None -> false
        | Some p -> p <> g.Player.Position
    
let RetryTimeout = RetryTimeout Connection.Tick    

let Walk =
    let WalkingRequired game =
        game.Goals.Position.IsSome &&
            game.Goals.Position.Value <> game.Player.Position
            
    let RequestWalk parent (game: Game) =
        let player = game.Player
        match game.Goals.Position with
        | Some (x, y) ->
            match Navigation.Pathfinding.FindPath
                  (Navigation.Maps.GetMapData game.Map) player.Position (x, y) with
            | [] -> parent Failure game
            | path ->
                let pos = path
                        |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                        |> List.last
                game.Request(Types.Request.RequestMove pos)                
                parent Success game
        | None -> parent Success game

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
    
let Disconnected parent (game:Game) =
    Logger.Warn ("Disconnected: {name}", game.Player.Name)
    parent (game, Success)
    
let Login parent game =
    game.Login game
    parent (game, Success)
