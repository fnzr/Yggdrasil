module Yggdrasil.Agent.Location

open System
open System.Threading
open NLog
open Yggdrasil.Navigation
open Yggdrasil.Utils
open Yggdrasil.Agent.Event

let Logger = LogManager.GetLogger("Location")

type Location (publish: GameEvent -> unit) =
    let walkLock = obj()
    let mutable map: string = ""
    let mutable position = 0, 0
    let mutable destination: (int * int) option = None    
    member this.Map
        with get() = map
        and set v = SetValue Logger &map v "MapChanged" |> ignore
    member this.Destination
        with get() = destination
        and set (v: (int * int) option) =
            if SetValue Logger &destination v "DestinationChanged" then
                let event = match destination with
                            | None -> Action Idle
                            | Some _ -> Action Moving
                publish event
    member this.Position
        with get() = position
        and set v = SetValue Logger &position v "PositionChanged" |> ignore
        
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.WalkLock = walkLock
    member this.DistanceTo point =
        Pathfinding.ManhattanDistance this.Position point
        
    member this.PathTo point leeway =
        let mapData = Maps.GetMapData this.Map
        Pathfinding.AStar mapData this.Position point leeway
        

let rec TryTakeStep (cancelToken: CancellationToken) (delay: int32) (location: Location) (path: (int * int) list) = async {
    do! Async.Sleep delay
    lock location.WalkLock
        (fun () ->
        if cancelToken.IsCancellationRequested then ()
        else
            location.Position <- fst path.Head, snd path.Head
            match path.Tail.Length with
            | 0 ->
                location.Destination <- None
                location.WalkCancellationToken <- None
            | _ ->
                //TODO handle change in speed
                Async.Start <| TryTakeStep cancelToken delay location path.Tail
            )
}

let StartWalk (location: Location) (origin: int * int) (destination: int * int) initialDelay speed =
    lock location.WalkLock
         (fun () ->
            match location.WalkCancellationToken with
            | Some (token) -> token.Cancel()
            | None -> ()
            location.Destination <- None
            
            let path = Pathfinding.AStar (Maps.GetMapData location.Map) origin destination 0
            if path.Length > 0 then
                location.Destination <- Some(destination)
                let tokenSource = new CancellationTokenSource()
                location.WalkCancellationToken <- Some(tokenSource)
                let naturalDelay = if initialDelay < 0L then 0 else Convert.ToInt32 initialDelay
                Async.Start <|
                async {
                    do! Async.Sleep naturalDelay
                    do! TryTakeStep tokenSource.Token speed location path
                }                
         )