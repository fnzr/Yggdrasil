module Yggdrasil.Reactive.Persona
open System
open FSharp.Control.Reactive
open Yggdrasil.Game
open Yggdrasil.Types
    
let ReactiveMovement origin map post =
    let source = Observable.distinctUntilChanged origin
    let SpeedObserver =
        Observable.choose (fun e -> match e with | Speed p -> Some p | _ -> None) source
        
    let SpeedSpan =
        SpeedObserver
        |> Observable.map (TimeSpan.FromMilliseconds)

    let DelayedStep (path: IObservable<#seq<_>>) =        
        Observable.flatmap
        <| fun s -> Observable.map (Observable.collect (fun q -> Observable.delay s <| Observable.single q)) path
        <| SpeedSpan
        
    let mapData = Yggdrasil.Navigation.Maps.GetMapData map
    let MovementObserver =
        Observable.choose (fun e -> match e with | Movement m -> Some m | _ -> None) source
        |> Observable.flatmap (fun data ->
            let path = Yggdrasil.Navigation.Pathfinding.FindPath mapData data.Origin data.Destination
            Observable.delay (TimeSpan.FromMilliseconds data.Delay) (Observable.single path)
            )
        |> DelayedStep
        
    let PositionObserver =
        Observable.choose (fun e -> match e with | ForcedPosition (x, y) -> Some [(x,y)] | _ -> None) source
        |> DelayedStep

    let StepObservable =
        Observable.merge PositionObserver MovementObserver
        |> Observable.switch
        
    [
     StepObservable.Subscribe(fun p -> post <| ForcedPosition p)
     SpeedObserver.Subscribe(fun s -> post <| Speed s)
    ]
    
type EntityHandler (persona: Entity, onUpdate) as __ =
    let reporter = Subject.broadcast
    let subscriptions = ReactiveMovement reporter persona.Map __.Update
    let _lock = obj()
    let mutable persona = persona
    interface IDisposable with
        member __.Dispose () = List.iter (fun (d: IDisposable) -> d.Dispose()) subscriptions
    member __.Reporter = reporter
    member __.Update message =
        lock _lock
        <| fun _ ->
            persona <-
                match message with
                | ForcedPosition p -> {persona with Position = p}
                | Speed s -> {persona with Speed = s}
                | _ -> persona
            onUpdate persona
