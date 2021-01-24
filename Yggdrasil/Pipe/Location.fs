module Yggdrasil.Pipe.Location

open System
open NLog
open FSharpPlus.Lens
open Yggdrasil.Game
open Yggdrasil.Game.Event
open Yggdrasil.Navigation
open Yggdrasil.Types

let Logger = LogManager.GetLogger "Location"

type Point = int * int
let rec TryTakeStep (actionId: Guid) (unitId: uint32)
    callback delay (path: Point list) (world: World) =
        match World.Unit world unitId with
        | None -> world
        | Some unit ->            
                if unit.ActionId = actionId then
                    let newUnit =
                        let u = {unit with Position = (fst path.Head, snd path.Head)}
                        match path.Tail with
                        | [] -> {u with Status = Yggdrasil.Game.Idle}
                        | tail ->
                            Async.Start <| async {
                                do! Async.Sleep (int u.Speed)
                                callback <| TryTakeStep actionId unitId callback delay tail
                            }; u
                    World.UpdateUnit newUnit world
                else world
     
let StartMove (unit: Unit) callback destination initialDelay (world: World) =
    let map = Maps.GetMapData world.Map
    let path = Pathfinding.FindPath map unit.Position destination 0        
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
    

let UnitWalk id origin dest startAt callback (world: World) =
    let delay = startAt - Connection.Tick() - world.TickOffset
    match World.Unit world id with
    | None -> Logger.Warn "Failed handling walk packet: unknown unit"; world
    | Some unit ->         
        let w = World.UpdateUnit {unit with Position = origin} world
        StartMove unit callback dest delay w
        w
    
        
let PlayerWalk origin dest startAt callback (world: World) =
    let delay = startAt - Connection.Tick() + world.TickOffset    
    let w = World.withPlayerPosition origin world
    StartMove world.Player.Unit callback dest delay w
    world
    
let MoveUnit (move: UnitMove) callback (world: World) =
    match World.Unit world move.aid with
    | None -> Logger.Warn ("Unhandled movement for {aid}", move.aid)
    | Some unit -> StartMove unit callback (int move.X, int move.Y) 0L world
    world
    
let MapChange position map (world: World) =
    world.Request DoneLoadingMap
    let unit = {world.Player.Unit with
                    Position = position
                    ActionId = Unit.Default.ActionId                    
                    Status = Unit.Default.Status
                    TargetOfSkills = Unit.Default.TargetOfSkills
                    Casting = Unit.Default.Casting}
    Maps.LoadMap map
    {world with
        IsMapReady = false
        Map = map
        NPCs = World.Default.NPCs
        Player = setl Player._Unit unit world.Player
    }
    
let MapProperty property flag world =
    //dont know what this is yet, but use it as flag
    {world with IsMapReady = true}