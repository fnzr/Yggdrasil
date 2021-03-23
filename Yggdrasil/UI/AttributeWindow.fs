module Yggdrasil.UI.AttributeWindow

open Mindmagma.Curses
open Yggdrasil.World.Attributes
open Yggdrasil.World.Stream
open Yggdrasil.UI.WindowType
open FSharp.Control.Reactive

let FillPrimaryAttributes win (attr: _ array) (costs: _ array) =
    NCurses.MoveWindowAddString(win, 0, 1, $"Primary:") |> ignore
    NCurses.MoveWindowAddString(win, 1, 2, $"STR {attr.[int Primary.STR]} ^{attr.[int Primary.STR]}") |> ignore
    NCurses.MoveWindowAddString(win, 2, 2, $"AGI {attr.[int Primary.AGI]} ^{attr.[int Primary.AGI]}") |> ignore
    NCurses.MoveWindowAddString(win, 3, 2, $"VIT {attr.[int Primary.VIT]} ^{attr.[int Primary.VIT]}") |> ignore
    NCurses.MoveWindowAddString(win, 4, 2, $"INT {attr.[int Primary.INT]} ^{attr.[int Primary.INT]}") |> ignore
    NCurses.MoveWindowAddString(win, 5, 2, $"DEX {attr.[int Primary.DEX]} ^{attr.[int Primary.DEX]}") |> ignore
    NCurses.MoveWindowAddString(win, 6, 2, $"LUK {attr.[int Primary.LUK]} ^{attr.[int Primary.LUK]}") |> ignore
    NCurses.MoveWindowAddString(win, 7, 3, $"Points: {attr.[int Primary.Points]}") |> ignore

let Window windowId windowStream messageStream =
    let attributeStream = PrimaryAttributesStream messageStream
    let attrCostStream = AttributeCostStream messageStream
    Observable.map
    <| fun window ->
        match window.Type with
        | AttributeWindow -> Some window
        | _ -> None
    <| windowStream
    |> Observable.combineLatest3 attributeStream attrCostStream
    |> (Observable.subscribe
        <| fun (attributes, costs, optWindow) ->
            match optWindow with
            | None -> ()
            | Some win ->
                FillPrimaryAttributes windowId attributes costs
                NCurses.WindowRefresh windowId |> ignore)

let Init window = ()
