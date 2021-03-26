module Yggdrasil.UI.AttributeWindow

open Mindmagma.Curses
open Yggdrasil.Observables
open Yggdrasil.UI.WindowType
open FSharp.Control.Reactive

let FillPrimaryAttributes win (attr: _ array) (costs: _ array) =
    NCurses.MoveWindowAddString(win, 0, 1, $"Primary:") |> ignore
    NCurses.MoveWindowAddString(win, 1, 2, $"STR {attr.[1]} ^{costs.[0]}") |> ignore
    NCurses.MoveWindowAddString(win, 2, 2, $"AGI {attr.[2]} ^{costs.[1]}") |> ignore
    NCurses.MoveWindowAddString(win, 3, 2, $"VIT {attr.[3]} ^{costs.[2]}") |> ignore
    NCurses.MoveWindowAddString(win, 4, 2, $"INT {attr.[4]} ^{costs.[3]}") |> ignore
    NCurses.MoveWindowAddString(win, 5, 2, $"DEX {attr.[5]} ^{costs.[4]}") |> ignore
    NCurses.MoveWindowAddString(win, 6, 2, $"LUK {attr.[6]} ^{costs.[5]}") |> ignore
    NCurses.MoveWindowAddString(win, 7, 3, $"Points: {attr.[0]}") |> ignore

let Window windowId windowStream messageStream =
    let attributeStream = ParameterStream messageStream
    let primaryStream = PrimaryParameterStream attributeStream
    let primaryCostStream = PrimaryParameterCostStream attributeStream
    Observable.map
    <| fun window ->
        match window.Type with
        | AttributeWindow -> Some window
        | _ -> None
    <| windowStream
    |> Observable.combineLatest3 primaryStream primaryCostStream
    |> (Observable.subscribe
        <| fun (attributes, costs, optWindow) ->
            match optWindow with
            | None -> ()
            | Some win ->
                FillPrimaryAttributes windowId attributes costs
                NCurses.WindowRefresh windowId |> ignore)

let Init window = ()
