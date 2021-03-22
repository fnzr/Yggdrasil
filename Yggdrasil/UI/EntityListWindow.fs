module Yggdrasil.UI.EntityListWindow

open System
open FSharp.Control.Reactive
open Mindmagma.Curses
open Yggdrasil.Types
open Yggdrasil.UI.WindowType
open Yggdrasil.World.Message
open Yggdrasil.World.Sensor

type Filter =
    | All
    | OnlyMob
    | OnlyNPC

let FilterMap =
    Map.empty
        .Add(All, [])
        .Add(OnlyMob, [EntityType.Monster])
        .Add(OnlyNPC, [EntityType.NPC])

let FillEntityLines maxRows maxCols window entities filter =
    let types = FilterMap.[filter]
    let seq = Seq.filter
                <| fun tracked -> if types.Length = 0 then true else List.contains tracked.Type types
                <| entities
    Seq.iter
    <| fun i ->
        let row = i + 1
        match Seq.tryItem i seq with
        | Some (tracked: TrackedEntity) ->
            NCurses.MoveWindowAddString(window, row, 3, (string tracked.Type).PadRight(7)) |> ignore
            NCurses.MoveWindowAddString(window, row, 15, tracked.Name.PadRight(25)) |> ignore
        | None ->
            NCurses.MoveWindowAddString(window, row, 1, " ".PadLeft(maxCols - 2)) |> ignore
    <| [0 .. maxRows]

let FilterStream inputStream =
    let filters = [All; OnlyMob; OnlyNPC]
    let mutable current = 0
    let filterKey = Convert.ToChar(9) |> string
    Observable.map
    <| fun input ->
        if input = filterKey then
            let next = current + 1
            current <- if next >= filters.Length then 0 else next
        filters.[current]
    <| inputStream
    |> Observable.startWith [All]

let Init window =
    NCurses.WindowBorder(window, '|', '|', ' ', '-', ' ', ' ', '+', '+')

let Window windowId windowStream maxRows playerId
    messageStream positionStream inputStream =
    let entityMapStream = EntityMapStream messageStream
    let entityPositionMapStream = EntityPositionMapStream playerId positionStream entityMapStream
    let filterStream = FilterStream inputStream
    let streams = Observable.combineLatest entityPositionMapStream filterStream
    Observable.map
    <| fun (window: Window) -> window.Type = EntityListWindow
    <| windowStream
    |> Observable.combineLatest streams
    |> (Observable.subscribe
        <| fun ((entities, filter), isEntityWindow) ->
            if isEntityWindow then
                FillEntityLines (maxRows - 2) 80 windowId entities filter
                NCurses.MoveWindowAddString(windowId, 0, 5, $"Entity List ({filter})") |> ignore
                NCurses.WindowRefresh windowId |> ignore)
