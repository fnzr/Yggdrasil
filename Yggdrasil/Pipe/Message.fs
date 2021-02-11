module Yggdrasil.Pipe.Message
open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Yggdrasil.Types

type Entity = {
    Id: Id
    Name: string
    Speed: float
    Map: string
    TickOffset: int64
}

type Location = {
    Id: Id
    Coordinates: Coordinates
}

type Health = {
    Id: Id
    MaxHP: int
    HP: int
}

type Movement = {
    Id: Id
    Origin: Coordinates
    Target: Coordinates
    Delay: float
}

type Message =
    | New of Entity
    | Location of Location
    | Movement of Movement 

let EntryPoint = Subject.broadcast

let NewEntity =
    Observable.choose
    <| fun m -> match m with New e -> Some e | _ -> None
    <| EntryPoint

let EntityObservable =
    Observable.groupBy
    <| fun (e: Entity) -> e.Id
    <| NewEntity

let PathObservable =
    Observable.choose
    <| fun m ->
        match m with
        | Movement m -> Some m
        | Location l ->
            Some {Id=l.Id; Origin=l.Coordinates; Target=l.Coordinates;Delay=0.0}
        | _ -> None
    <| EntryPoint
    |> Observable.groupBy (fun m -> m.Id)
    
let CreateSteps entity move =
    let data = Yggdrasil.Navigation.Maps.GetMapData entity.Map
    let path = Yggdrasil.Navigation.Pathfinding.FindPath data move.Origin move.Target
    let delay = TimeSpan.FromMilliseconds entity.Speed
    Observable.delay
    <| (TimeSpan.FromMilliseconds move.Delay)
    <| (Observable.collect
        <| fun pos -> Observable.delay delay
                    <| Observable.single {Id=entity.Id; Coordinates=pos}
        <| path)
    
let EntityLocation =
    Observable.flatmap
    <| fun (groupedEntity: IGroupedObservable<Id,Entity>) ->
        let groupedMovement =
                Observable.filter
                <| fun (grpMove: IGroupedObservable<_,_>) -> grpMove.Key = groupedEntity.Key
                <| PathObservable
        
        Observable.flatmap
        <| fun entity ->
            Observable.flatmap
                <| Observable.map (CreateSteps entity)
                <| groupedMovement
        <| groupedEntity
        |> Observable.switch
    <| EntityObservable
