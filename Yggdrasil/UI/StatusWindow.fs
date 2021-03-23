module Yggdrasil.UI.StatusWindow

open System
open FSharp.Control.Reactive
open Mindmagma.Curses
open Yggdrasil.World.Types
open Yggdrasil.World.Stream

type StatusInfo =
    | Connection of string
    | Position of string
    | BaseExp of string
    | JobExp of string

let ConnectedStream (messageStream: IObservable<_>) =
    Observable.choose
    <| fun m -> match m with | Connected c -> Some c | _ -> None
    <| messageStream
    |> Observable.startWith [false]
    |> (Observable.map
        <| fun isConnected -> (if isConnected then "Online" else "Offline").PadRight(7) |> Connection)

let CurrentMapStream playerId (messageStream: IObservable<_>) (positionStream: IObservable<_>) =
    let positionStream = SelfPositionStream playerId positionStream
    Observable.choose
    <| fun m -> match m with | MapChanged m -> Some m | _ -> None
    <| messageStream
    |> Observable.combineLatest positionStream
    |> (Observable.map
            <| fun (pos, map) -> $"{map} {(string pos.Coordinates).PadLeft(11)}".PadLeft(36) |> Position)

let JobExpStream messageStream =
    let nextLevel = NextJobLevelExpStream messageStream
    let currentLevel = JobLevelExpStream messageStream
    Observable.map
    <| fun (next, current: int64) ->
        let value = Math.Round(float (100.0 * (float current) / (float next)), 2)
        $"Job: {value}%%".PadRight(12) |> JobExp
    <| Observable.combineLatest nextLevel currentLevel

let BaseExpStream messageStream =
    let nextLevel = NextBaseLevelExpStream messageStream
    let currentLevel = BaseLevelExpStream messageStream
    Observable.map
    <| fun (next, current: int64) ->
        let value = Math.Round(float (100.0 * (float current) / (float next)), 2)
        $"Base: {value}%%".PadRight(12) |> BaseExp
    <| Observable.combineLatest nextLevel currentLevel

let Window window cols messageStream positionStream name id =
    NCurses.WindowBorder(window, '|', '|', '-', ' ', '+', '+', ' ', ' ')
    NCurses.MoveWindowAddString(window, 1, 12, $"{name} ({id})") |> ignore
    Observable.mergeSeq
        [CurrentMapStream id messageStream positionStream
         ConnectedStream messageStream
         BaseExpStream messageStream
         JobExpStream messageStream]
    |> Observable.subscribe (fun update ->
        match update with
        | Connection c -> NCurses.MoveWindowAddString(window, 1, 2, c)
        | Position p -> NCurses.MoveWindowAddString(window, 2, cols - p.Length - 1, p)
        | BaseExp e -> NCurses.MoveWindowAddString(window, 2, 2, e)
        | JobExp e -> NCurses.MoveWindowAddString(window, 2, 15, e)
        |> ignore
        NCurses.WindowRefresh(window) |> ignore)
