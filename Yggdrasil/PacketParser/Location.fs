module Yggdrasil.PacketParser.Location

open System
open NLog
open Yggdrasil.Game
open Yggdrasil.Game.Player
open Yggdrasil.Navigation
open Yggdrasil.Utils
open Yggdrasil.PacketParser.Decoder
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
                        | [] -> {u with Status = Idle}
                        | tail ->
                            Async.Start <| async {
                                do! Async.Sleep (int u.Speed)
                                callback <| TryTakeStep actionId unitId callback delay path.Tail
                            }; u
                    World.UpdateUnit unit world
                else world
     
let StartMove (world: World) (unit: Unit) callback destination initialDelay =
    let map = Maps.GetMapData world.Map
    let path = Pathfinding.FindPath map unit.Position destination 0        
    if path.Length > 0 then
        let id = Guid.NewGuid()
        let delay = if initialDelay < 0L then 0 else Convert.ToInt32 initialDelay
        Async.Start <| async {
            callback <| World.UpdateUnit {unit with ActionId = id}
            do! Async.Sleep delay
            callback <| TryTakeStep id unit.Id callback (int unit.Speed) path
        }        
    world

let UnitWalk (data: byte[]) callback (world: World) =
    let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[4..]
    let delay = int64 (ToUInt32 data) - Connection.Tick - world.TickOffset
    match World.Unit world <| ToUInt32 data with
    | None -> Logger.Warn "Failed handling walk packet: unknown unit"; world
    | Some unit ->
        let w = World.withPlayerPosition (int x0, int y0) world
        StartMove w unit callback (int x1, int y1) delay        
        
let PlayerWalk (data: byte[]) callback (world: World) =
    let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[4..]
    let delay = int64 (ToUInt32 data) - Connection.Tick - world.TickOffset    
    let w = World.withPlayerPosition (int x0, int y0) world
    StartMove w world.Player.Unit callback (int x1, int y1) delay
    
let MoveUnit data callback (world: World) =
    let move = MakeRecord<UnitMove> data
    match World.Unit world move.aid with
    | None -> Logger.Warn ("Unhandled movement for {aid}", move.aid); world
    | Some unit -> StartMove world unit callback (int move.X, int move.Y) 0L
    
let MapChange (data: byte[]) (world: World) =
    world.Player.Dispatch DoneLoadingMap
    let position = (data.[16..] |> ToUInt16 |> int,
                    data.[18..] |> ToUInt16 |> int)
    {world with
        Map = (let gatFile = ToString data.[..15]
               gatFile.Substring(0, gatFile.Length - 4))        
        NPCs = World.Default.NPCs
        Player = {world.Player with
                    ActionId = Player.Default.ActionId
                    Status = Player.Default.Status
                    TargetOfSkills = Player.Default.TargetOfSkills
                    Casting = Player.Default.Casting}
    }