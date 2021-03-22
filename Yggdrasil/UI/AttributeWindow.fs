module Yggdrasil.UI.AttributeWindow

open Mindmagma.Curses
open Yggdrasil.World.Sensor
open Yggdrasil.World.Message
open Yggdrasil.UI.WindowType
open FSharp.Control.Reactive

let FillPrimaryAttributes win (attr: _ array) =
    NCurses.MoveWindowAddString(win, 0, 1, $"Primary:") |> ignore
    NCurses.MoveWindowAddString(win, 1, 2, $"STR {attr.[STR]}") |> ignore
    NCurses.MoveWindowAddString(win, 2, 2, $"AGI {attr.[AGI]}") |> ignore
    NCurses.MoveWindowAddString(win, 3, 2, $"VIT {attr.[VIT]}") |> ignore
    NCurses.MoveWindowAddString(win, 4, 2, $"INT {attr.[INT]}") |> ignore
    NCurses.MoveWindowAddString(win, 5, 2, $"DEX {attr.[DEX]}") |> ignore
    NCurses.MoveWindowAddString(win, 6, 2, $"LUK {attr.[LUK]}") |> ignore

let Window windowId windowStream messageStream =
    let attributeStream = PrimaryAttributesStream messageStream
    Observable.map
    <| fun window ->
        match window.Type with
        | AttributeWindow -> Some window
        | _ -> None
    <| windowStream
    |> Observable.combineLatest attributeStream
    |> (Observable.subscribe
        <| fun (attributes, optWindow) ->
            match optWindow with
            | None -> ()
            | Some win ->
                FillPrimaryAttributes windowId attributes
                NCurses.WindowRefresh windowId |> ignore)

let Init window = ()
