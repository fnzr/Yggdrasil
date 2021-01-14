namespace Yggdrasil.Game

open System
open System.Threading
open NLog
open Yggdrasil.Navigation
open Yggdrasil.Game.Event

module Movement =
    let Logger = LogManager.GetLogger "Location"
    type Point = int * int

    type WalkInfo =
        | NewPosition of Point
        | DestinationReached
    let rec TryTakeStep (walkFn: WalkInfo -> unit) (token: CancellationToken)
        (path: Point list) delay = async {
            do! Async.Sleep delay
            if token.IsCancellationRequested then ()
            else
                walkFn <|
                    match path.Tail with
                    | [] ->                        
                        DestinationReached
                    | tail ->
                        Async.Start <| TryTakeStep walkFn token tail delay
                        NewPosition (fst path.Head, snd path.Head)
        } 
    let StartMove mapName walkFn origin destination initialDelay speed =
        let map = Maps.GetMapData mapName
        let path = Pathfinding.FindPath map origin destination 0        
        if path.Length > 0 then
            let tokenSource = new CancellationTokenSource()
            let delay = if initialDelay < 0L then 0 else Convert.ToInt32 initialDelay
            Async.Start <| async {
                do! Async.Sleep delay
                do! TryTakeStep walkFn tokenSource.Token path speed
            }
            Some(tokenSource)
        else None

    let Walk updatePosition eventHandler =
        eventHandler <| Action Moving
        fun walkInfo ->
            match walkInfo with
            | DestinationReached ->                
                eventHandler <| Action Idle
            | NewPosition (x, y) -> updatePosition (x, y)

