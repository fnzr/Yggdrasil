module Yggdrasil.Pipe.Location

open System
open NLog
open FSharpPlus.Lens
open Yggdrasil.Game
open Yggdrasil.Navigation
open Yggdrasil.Types
open Yggdrasil.Utils

let Logger = LogManager.GetLogger "Location"
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

type Point = int16 * int16
let rec TryTakeStep (actionId: Guid) (unitId: uint32)
    callback delay (path: Point list) (game: Game) =
        match game.Units.TryFind unitId with
        | None -> game
        | Some unit ->            
                if unit.ActionId = actionId then
                    let newUnit =
                        let u = {unit with Position = (fst path.Head, snd path.Head)}
                        match path.Tail with
                        | [] -> {u with Status = Idle}
                        | tail ->
                            Async.Start <| async {
                                do! Async.Sleep (int u.Speed)
                                callback <| TryTakeStep actionId unitId callback delay tail
                            }; u
                    Tracer.Send Game.UpdateUnit newUnit game
                else Tracer.Send (game, "Cancelled Walk")
     
let StartMove (unit: Unit) callback destination initialDelay (game: Game) =
    let map = Maps.GetMapData game.Map
    let path = Pathfinding.FindPath map unit.Position destination
    if path.Length > 0 then
        let id = Guid.NewGuid()
        let delay = if initialDelay < 0L then 0 else Convert.ToInt32 initialDelay
        callback <| Game.UpdateUnit {unit with ActionId = id; Status = Walking}
        Async.Start <| async {
            do! Async.Sleep delay
            callback <| TryTakeStep id unit.Id callback (int unit.Speed) path
        }
    else
        Logger.Error("Unit {name} ({aid}): Could not find walk path: {source} => {dest}",
                     unit.Name, unit.Id, unit.Position, destination)
    

let UnitWalk id origin dest startAt callback (game: Game) =
    let delay = startAt - Connection.Tick() - game.TickOffset
    match game.Units.TryFind id with
    | None -> Logger.Warn "Failed handling walk packet: unknown unit"; game
    | Some unit ->         
        let w = game.UpdateUnit {unit with Position = origin}
        StartMove unit callback dest delay w
        Tracer.Send w
    
        
let PlayerWalk origin dest startAt callback (game: Game) =
    let delay = startAt - Connection.Tick() + game.TickOffset
    
    let w = game.UpdateUnit {game.Player with Position = origin}
    StartMove game.Player callback dest delay w
    Tracer.Send game
    
let MoveUnit (move: UnitMove) callback (game: Game) =
    match game.Units.TryFind move.aid with
    | None -> Logger.Warn ("Unhandled movement for {aid}", move.aid)
    | Some unit -> StartMove unit callback (move.X, move.Y) 0L game
    Tracer.Send game
    
let MapChange position map (game: Game) =
    game.Request DoneLoadingMap
    let player = {game.Player with
                    Position = position
                    ActionId = Unit.Default.ActionId                    
                    Status = Unit.Default.Status
                    TargetOfSkills = Unit.Default.TargetOfSkills
                    Casting = Unit.Default.Casting}
    Maps.LoadMap map
    Tracer.Send
        {game with
            IsMapReady = false
            Map = map
            Units = Map.empty.Add(game.Player.Id, player)
        }
    
    
let MapProperty property flag game =
    //dont know what this is yet, but use it as flag
    {game with IsMapReady = true}
