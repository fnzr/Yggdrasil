module Yggdrasil.World.Message
open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Yggdrasil.Navigation
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

let CreateObservableGraph selfId entryPoint =
    let NewEntity =
        Observable.choose
            <| fun m -> match m with New e -> Some e | _ -> None
            <| entryPoint

    let EntityHealth =
        Observable.choose
            <| fun m -> match m with Health e -> Some e | _ -> None
            <| entryPoint

    let PositionMessage =
        Observable.groupBy
        <| fun (l: Location) -> l.Id
        <| (Observable.choose
            <| fun m ->
                match m with
                | Location l -> Some l
                | _ -> None
            <| entryPoint)
        |> Observable.single

    let EntityMovement = MovementObservable entryPoint

    let LocationUpdate =
        //Observable.map
        //<| fun p ->
          //  Observable.map (fun l -> (l :> IObservable<_>)) p
            //|> Observable.switch
        Observable.merge PositionMessage EntityMovement
        |> Observable.switch

    let SelfLocation =
        Observable.filter
        <| fun (l: IGroupedObservable<_,_>) -> l.Key = selfId
        <| LocationUpdate

    let LocationsAndSelf =
        Observable.map
        <| fun l -> Observable.combineLatest (Observable.single l) SelfLocation
        <| LocationUpdate
        |> Observable.switch


    LocationUpdate.Subscribe(printfn "%A") |> ignore
    //SelfLocation.Subscribe(printfn "%A")
    LocationsAndSelf.Subscribe(printfn "%A") |> ignore

    let LocationUpdate =
        Observable.map
        <| fun (location, self) ->
            let visible =
                match location.Position with
                   | Known (x, y) ->
                       let dist = Pathfinding.DistanceTo
                                      (x, y)
                                      (Position.Value self.Position)
                       dist <= 30s
                   | Unknown -> false
            if visible
                then (fun (m: Map<_,_>) -> m.Add(location.Id, location))
                else (fun (m: Map<_,_>) -> m.Remove location.Id)
        <| LocationsAndSelf

    let LocationsMap =
        Observable.scanInit
        <| Map.empty
        <| fun map op -> op map
        <| LocationUpdate

    let KnownEntities =
        Observable.scanInit
        <| Map.empty
        <| fun (map: Map<_, Entity>) (entity: Entity) -> map.Add(entity.Id, entity)
        <| NewEntity
    {
        Messages = entryPoint
        Entities = NewEntity
        //Locations = LocationUpdate
        Health = EntityHealth
        KnownLocations = LocationsMap
        KnownEntities = KnownEntities
    }
