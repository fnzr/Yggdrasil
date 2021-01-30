module Yggdrasil.Behavior.Propagators

open System.Reactive
open Yggdrasil
open FSharp.Control.Reactive
open Yggdrasil.Game
let PlayerId = Subject.behavior 0u

//let PlayerIdMapper = Observable.filter(fun i -> printfn "Observed: %A" i; false) PlayerId

//let PlayerIdWatcher = Observable.add (fun i -> printfn "22: %A" i;) PlayerIdMapper

let ByUnitId id =
        Observable.choose(fun (i, e) -> if i = id then Some e else None)
    
let UnitPosition =
    Observable.choose <|
    (fun u ->
        match u with
        | UnitPosition (id, pos) -> Some (id, pos)
        | _ -> None)    

let SetupPropagators (game: GameO) =
    let playerPos =
        game.GameUpdate |>
        UnitPosition |>
        ByUnitId game.PlayerId
    let _ = playerPos.Subscribe(fun a -> printfn "Player pos: %A" a)
    printfn "Done setting up propags"
        
    //let PrintPosition playe