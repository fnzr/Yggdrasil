module Yggdrasil.Agent.Location

open NLog
open Yggdrasil.Navigation
open Yggdrasil.Utils
open Yggdrasil.Agent.Event
type Location (publish: GameEvent -> unit) =
    let mutable map: string = ""
    let mutable position = 0, 0
    let mutable destination: (int * int) option = None
    let logger = LogManager.GetLogger("Location")
    member this.Map
        with get() = map
        and set v = SetValue logger &map v "MapChanged" |> ignore
    member this.Destination
        with get() = destination
        and set (v: (int * int) option) =
            if SetValue logger &destination v "DestinationChanged" then
                let event = match destination with
                            | None -> Action Idle
                            | Some _ -> Action Moving
                publish event
    member this.Position
        with get() = position
        and set v = SetValue logger &position v "PositionChanged" |> ignore
        
    member this.DistanceTo point =
        Pathfinding.ManhattanDistance this.Position point
        
    member this.PathTo point leeway =
        let mapData = Maps.GetMapData this.Map
        Pathfinding.AStar mapData this.Position point leeway