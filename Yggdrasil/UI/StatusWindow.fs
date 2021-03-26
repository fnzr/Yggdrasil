module Yggdrasil.UI.StatusWindow

open System
open FSharp.Control.Reactive
open Mindmagma.Curses
open Yggdrasil.Game
open Yggdrasil.Observables

type StatusInfo =
    | Connection of string
    | Position of string
    | BaseExp of string
    | JobExp of string
    | HP of string
    | SP of string

let ConnectedStream (messageStream: IObservable<_>) =
    Observable.choose
    <| fun m -> match m with | Connected c -> Some c | _ -> None
    <| messageStream
    |> Observable.startWith [false]
    |> (Observable.map
        <| fun isConnected -> (if isConnected then "Online" else "Offline").PadRight(7) |> Connection)

let HPPercentStream selfId messageStream =
    let hpStream =
        Observable.choose
        <| fun (id, v) -> if selfId = id then Some v else None
        <| HPStream messageStream
    let maxStream =
        Observable.choose
        <| fun (id, v) -> if selfId = id then Some v else None
        <| MaxHPStream messageStream
    Observable.map
    <| fun (hp, max) ->
        let value = Math.Round(float (100.0 * (float hp) / (float max)), 2)
        $"HP: {value}%%".PadRight(12) |> HP
    <| Observable.combineLatest hpStream maxStream

let SPPercentStream parameterStream =
    Observable.map
    <| fun (sp, max) ->
        let value = Math.Round(float (100.0 * (float sp) / (float max)), 2)
        $"SP: {value}%%".PadRight(12) |> SP
    <| SPStream parameterStream

let CurrentMapStream playerId (messageStream: IObservable<_>) (positionStream: IObservable<_>) =
    let positionStream = SelfPositionStream playerId positionStream
    Observable.choose
    <| fun m -> match m with | MapChanged m -> Some m | _ -> None
    <| messageStream
    |> Observable.combineLatest positionStream
    |> (Observable.map
            <| fun (pos, map) -> $"{map} {(string pos.Coordinates).PadLeft(11)}".PadLeft(36) |> Position)

let JobExpStream messageStream parameterStream =
    let nextLevel = NextJobLevelExpStream messageStream
    let currentExp = JobLevelExpStream messageStream
    let job = JobLevelStream parameterStream
    Observable.map
    <| fun (level, next, current: int64) ->
        let value = Math.Round(float (100.0 * (float current) / (float next)), 2)
        $"Job: {level} ({value}%%)".PadRight(12) |> JobExp
    <| Observable.combineLatest3 job nextLevel currentExp

let BaseExpStream messageStream parameterStream =
    let nextLevel = NextBaseLevelExpStream messageStream
    let currentLevel = BaseLevelExpStream messageStream
    let baseLevel = BaseLevelStream parameterStream
    Observable.map
    <| fun (level, next, current: int64) ->
        let value = Math.Round(float (100.0 * (float current) / (float next)), 2)
        $"Base: {level} ({value}%%)".PadRight(12) |> BaseExp
    <| Observable.combineLatest3 baseLevel nextLevel currentLevel

let Window window cols messageStream positionStream name id =
    let parameterStream = ParameterStream messageStream
    NCurses.WindowBorder(window, '|', '|', '-', ' ', '+', '+', ' ', ' ')
    NCurses.MoveWindowAddString(window, 1, 12, $"{name} ({id})") |> ignore
    Observable.mergeSeq
        [CurrentMapStream id messageStream positionStream
         ConnectedStream messageStream
         BaseExpStream messageStream parameterStream
         JobExpStream messageStream parameterStream
         HPPercentStream id messageStream
         SPPercentStream parameterStream]
    |> Observable.subscribe (fun update ->
        match update with
        | Connection c -> NCurses.MoveWindowAddString(window, 1, 2, c)
        | Position p -> NCurses.MoveWindowAddString(window, 2, cols - p.Length - 2, p)
        | BaseExp e -> NCurses.MoveWindowAddString(window, 2, 2, e)
        | JobExp e -> NCurses.MoveWindowAddString(window, 2, 15, e)
        | HP s -> NCurses.MoveWindowAddString(window, 2, 28, s)
        | SP s -> NCurses.MoveWindowAddString(window, 2, 39, s)
        |> ignore
        NCurses.WindowRefresh(window) |> ignore)
