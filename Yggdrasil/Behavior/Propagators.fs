module Yggdrasil.Behavior.Propagators

open System
open System.Reactive
open Yggdrasil
open FSharp.Control.Reactive.Builders
open FSharp.Control.Reactive
open Yggdrasil.Game

let MapObservable source =
    Observable.choose (fun e -> match e with | MapChanged m -> Some m | _ -> None) source
let DestObservable source =
    Observable.choose (fun e -> match e with | UnitMovement (i, m) -> Some (i, m) | _ -> None) source
let SpeedObservable source =
    Observable.choose (fun e -> match e with | UnitSpeed (i, m) -> Some (i, m) | _ -> None) source
    
let SpeedSpan source =
    SpeedObservable source
    |> Observable.map (fun (i, s) -> (i, TimeSpan.FromMilliseconds (float s)))

let DelayedStep source path =
    Observable.combineLatest (SpeedSpan source) path
    |> Observable.map (fun (a, b) ->
        Observable.collect (fun p -> (iObservable.delay a <| Observable.single p) b)
    
let PathObservable source =
    Observable.combineLatest (MapObservable source) (DestObservable source)
    |> Observable.map (fun (m, d) ->
        let mapData = Navigation.Maps.GetMapData m
        Navigation.Pathfinding.FindPath printfn "Finding a path in {%s}: {%A}" m d; Observable.empty)
    
let WalkObservable source =
    Observable.choose (fun e -> match e with | Path p -> Some p | _ -> None) source
    |> DelayedStep source
    
let PositionUpdate source =
    Observable.choose (fun e -> match e with | Position (x, y) -> Some [(x,y)] | _ -> None) source
    |> DelayedStep source
    
let StepObservable source =
    Observable.merge
    <| (PositionUpdate source)
    <| (WalkObservable source)
    |> Observable.switch

let SetupPropagators (game: GameO) =
    let root = game.GameUpdate
    let playerPos =
        game.GameUpdate |>
        UnitPosition game.PlayerId
    let _ = playerPos.Subscribe(fun a -> printfn "Player pos: %A" a)
    printfn "Done setting up propags"
        
    
    //let PrintPosition playe