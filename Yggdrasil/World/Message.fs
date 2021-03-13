module Yggdrasil.World.Message
open System
open FSharp.Control.Reactive
open Yggdrasil.Navigation.Maps
open Yggdrasil.Types
open Yggdrasil.World.Sensor

type PacketMessage =
    | Message of Message
    | Messages of Message list
    | Skip
    | Unhandled of uint16
    
let MovementObservable entryPoint =
    let MovementMessage =
        Observable.groupBy
        <| fun (m: Movement) -> m.Id
        <| (Observable.choose
            <| fun m ->
                match m with
                | Movement k -> Some k
                | _ -> None
            <| entryPoint
            |> Observable.distinctUntilChanged)
        
    Observable.map
    <| fun groupedMove ->
        Observable.map
        <| fun move ->
            let path = Yggdrasil.Navigation.Pathfinding.FindPath
                       <| move.Map.Data
                       <| Position.Value move.Origin
                       <| Position.Value move.Target
            let delay = TimeSpan.FromMilliseconds move.Speed
            Observable.delay
            <| TimeSpan.FromMilliseconds move.Delay
            <| (Observable.collect
                <| fun pos -> Observable.delay delay
                            <| Observable.single {Id=move.Id; Map=move.Map;Position=Known pos}
                <| path)
        <| groupedMove
        |> Observable.switch
        |> Observable.groupBy (fun m -> m.Id)
    <| MovementMessage

let CreateObservableGraph entryPoint =
    let NewEntity =
        Observable.choose
            <| fun m -> match m with New e -> Some e | _ -> None
            <| entryPoint
        |> Observable.distinctUntilChanged
        
    let EntityHealth =
        Observable.choose
            <| fun m -> match m with Health e -> Some e | _ -> None
            <| entryPoint
        |> Observable.distinctUntilChanged

    let PositionMessage =
        Observable.groupBy
        <| fun (l: Location) -> l.Id
        <| (Observable.choose
            <| fun m ->
                match m with
                | Location l -> Some l
                | _ -> None
            <| entryPoint
            |> Observable.distinctUntilChanged)
        |> Observable.single
        
    let EntityMovement = MovementObservable entryPoint
        
    let EntityLocation =
        Observable.map
        <| fun p ->
            Observable.map (fun l -> (l :> IObservable<_>)) p
            |> Observable.switch
        <| Observable.merge PositionMessage EntityMovement
        |> Observable.flatmap id
         
    {
        Messages = entryPoint
        Entities = NewEntity
        Locations = EntityLocation
        Health = EntityHealth
    }
