module Yggdrasil.World.Location

open System
open System.Threading
open Yggdrasil.Navigation

type Point = int * int

type Position =
    | NewPosition of Point
    | DestinationReached
let rec TryTakeStep (updatePosition: Position -> unit) (token: CancellationToken)
    (path: Point list) delay = async {
        do! Async.Sleep delay
        if token.IsCancellationRequested then ()
        else
            updatePosition <|
                match path.Tail with
                | [] ->  DestinationReached
                | tail ->
                    Async.Start <| TryTakeStep updatePosition token tail delay
                    NewPosition (fst path.Head, snd path.Head)
    } 
let StartMove map updatePosition origin destination initialDelay speed =
    let path = Pathfinding.AStar map origin destination 0
    if path.Length > 0 then
        let tokenSource = new CancellationTokenSource()
        let delay = if initialDelay < 0L then 0 else Convert.ToInt32 initialDelay
        Async.Start <| async {
            do! Async.Sleep delay
            do! TryTakeStep updatePosition tokenSource.Token path speed
        }
        Some(tokenSource)
    else None
