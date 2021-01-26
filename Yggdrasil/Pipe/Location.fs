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

type Point = int * int
let rec TryTakeStep (actionId: Guid) (unitId: uint32)
    callback delay (path: Point list) (world: Game) =
        match world.Units.TryFind unitId with
        | None -> world
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
                    Tracer.Send World.UpdateUnit newUnit world
                else Tracer.Send (world, "Cancelled Walk")
     
let StartMove (unit: Unit) callback destination initialDelay (world: Game) =
    let map = Maps.GetMapData world.Map
    let path = Pathfinding.FindPath map unit.Position destination
    if path.Length > 0 then
        let id = Guid.NewGuid()
        let delay = if initialDelay < 0L then 0 else Convert.ToInt32 initialDelay
        callback <| World.UpdateUnit {unit with ActionId = id; Status = Walking}
        Async.Start <| async {
            do! Async.Sleep delay
            callback <| TryTakeStep id unit.Id callback (int unit.Speed) path
        }
    else
        Logger.Error("Unit {name} ({aid}): Could not find walk path: {source} => {dest}",
                     unit.Name, unit.Id, unit.Position, destination)
    

let UnitWalk id origin dest startAt callback (world: Game) =
    let delay = startAt - Connection.Tick() - world.TickOffset
    match world.Units.TryFind id with
    | None -> Logger.Warn "Failed handling walk packet: unknown unit"; world
    | Some unit ->         
        let w = world.UpdateUnit {unit with Position = origin}
        StartMove unit callback dest delay w
        Tracer.Send w
    
        
let PlayerWalk origin dest startAt callback (world: Game) =
    let delay = startAt - Connection.Tick() + world.TickOffset
    
    let w = world.UpdateUnit {world.Player with Position = origin}
    StartMove world.Player callback dest delay w
    Tracer.Send world
    
let MoveUnit (move: UnitMove) callback (world: Game) =
    match world.Units.TryFind move.aid with
    | None -> Logger.Warn ("Unhandled movement for {aid}", move.aid)
    | Some unit -> StartMove unit callback (int move.X, int move.Y) 0L world
    Tracer.Send world
    
let MapChange position map (world: Game) =
    world.Request DoneLoadingMap
    let player = {world.Player with
                    Position = position
                    ActionId = Unit.Default.ActionId                    
                    Status = Unit.Default.Status
                    TargetOfSkills = Unit.Default.TargetOfSkills
                    Casting = Unit.Default.Casting}
    Maps.LoadMap map
    Tracer.Send
        {world with
            IsMapReady = false
            Map = map
            Units = Map.empty.Add(world.Player.Id, player)
        }
    
    
let MapProperty property flag world =
    //dont know what this is yet, but use it as flag
    {world with IsMapReady = true}
