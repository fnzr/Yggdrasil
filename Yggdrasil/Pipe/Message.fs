module Yggdrasil.Pipe.Message
open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Yggdrasil.Navigation.Maps
open Yggdrasil.Types

type Position =
    | Known of int16 * int16
    | Unknown
    
module Position =
    let Value pos =
        match pos with
        | Known (a, b) -> (a, b)
        | Unknown -> invalidArg "pos" "Unknown position"

type Entity = {
    Id: Id
    Type: EntityType
    Name: string
}

type Location =
    {
        Id: Id
        Map: Map
        Position: Position
    }
    static member Unknown = -1s, -1s

type Movement = {
    Id: Id
    Map: Map
    Speed: float
    Origin: Position
    Target: Position
    Delay: float
}
    

type Message =
    | New of Entity
    | Location of Location
    | Movement of Movement 

type EntityObservables = {
    Entities: IObservable<Entity>
    Locations: IObservable<Location>
}

let CreateObservables entryPoint =
    let NewEntity =
        Observable.choose
            <| fun m -> match m with New e -> Some e | _ -> None
            <| entryPoint
        |> Observable.distinctUntilChanged

    let MovementMessage =
        Observable.groupBy
        <| fun m -> m.Id
        <| (Observable.choose
            <| fun m ->
                match m with
                | Movement k -> Some k
                | _ -> None
            <| entryPoint
            |> Observable.distinctUntilChanged)
        
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
        
    let PathObservable =
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
        
    let EntityLocation =
        Observable.map
        <| fun p ->
            Observable.map (fun l -> (l :> IObservable<_>)) p
            |> Observable.switch
        <| Observable.merge PositionMessage PathObservable
        |> Observable.flatmap id
         
    {
        Entities = NewEntity
        Locations = EntityLocation
    }
