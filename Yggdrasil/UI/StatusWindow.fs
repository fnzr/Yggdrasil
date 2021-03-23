module Yggdrasil.UI.StatusWindow

open System
open FSharp.Control.Reactive
open Mindmagma.Curses
open Yggdrasil.World.Types
open Yggdrasil.World.Stream

type StatusInfo =
    | Connection of string
    | Position of string

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

let Window window cols messageStream positionStream name id =
    NCurses.WindowBorder(window, '|', '|', '-', ' ', '+', '+', ' ', ' ')
    NCurses.MoveWindowAddString(window, 1, 12, $"{name} ({id})") |> ignore
    Observable.merge (CurrentMapStream id messageStream positionStream) (ConnectedStream messageStream)
    |> Observable.subscribe (fun update ->
        match update with
        | Connection c -> NCurses.MoveWindowAddString(window, 1, 2, c)
        | Position p -> NCurses.MoveWindowAddString(window, 1, cols - p.Length - 1, p)
        |> ignore
        NCurses.WindowRefresh(window) |> ignore)
