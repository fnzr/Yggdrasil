module Yggdrasil.Observables
open System
open System.Collections.Concurrent
open FSharp.Control.Reactive
open Yggdrasil.Navigation
open Yggdrasil.Navigation.Maps
open Yggdrasil.Types
open Yggdrasil.Game

module Observable =
    let combineLatest3 a b c =
        Observable.combineLatest a b
        |> Observable.combineLatest c
        |> Observable.map (fun (c, (a, b)) -> a, b, c)

    let tap fn = Observable.map (fun o -> fn o; o)

let ParameterStream messageStream =
    Observable.choose
    <| fun m -> match m with | Parameter p -> Some p | _ -> None
    <| messageStream
    |> (Observable.scanInit
        //eh...
        <| Array.zeroCreate (Attribute.GetNames(typeof<Attribute>).Length)
        <| fun parameters list ->
            List.iter (fun (i, v) -> parameters.[int i] <- v) list; parameters)

let BaseLevelStream parameterStream =
    Observable.map
    <| fun (ps: array<_>) -> ps.[int Attribute.BaseLevel]
    <| parameterStream
    |> Observable.distinctUntilChanged

let JobLevelStream parameterStream =
    Observable.map
    <| fun (ps: array<_>) -> ps.[int Attribute.JobLevel]
    <| parameterStream
    |> Observable.distinctUntilChanged

let PrimaryParameterStream parameterStream =
    Observable.map
    <| fun (param: array<_>) -> param.[int Attribute.StatusPoints .. int Attribute.LUK]
    <| parameterStream
    |> Observable.distinctUntilChanged

let PrimaryParameterCostStream parameterStream =
    Observable.map
    <| fun (param: array<_>) -> param.[int Attribute.USTR .. int Attribute.ULUK]
    <| parameterStream
    |> Observable.distinctUntilChanged

let HPStream messageStream =
    Observable.choose
    <| fun m -> match m with | HP (id, v) -> Some (id, v) | _ -> None
    <| messageStream

let MaxHPStream messageStream =
    Observable.choose
    <| fun m -> match m with | MaxHP (id, v) -> Some (id, v) | _ -> None
    <| messageStream

let SPStream parameterStream =
    Observable.map
    <| fun (param: array<_>) -> param.[int Attribute.SP], param.[int Attribute.MaxSP]
    <| parameterStream

let PositionStream time messageStream =
    let latestPositionTime = ConcurrentDictionary<_,_>()
    let mutable currentMap = WalkableMap 0us
    let speedMap = ConcurrentDictionary<Id, float>()
    let PositionMessage =
        Observable.choose
            <| fun m ->
                match m with
                | Speed (id, speed) -> speedMap.[id] <- speed; None
                | Position p ->
                    let t = time()
                    latestPositionTime.[p.Id] <- t
                    Some (t, p.Id, p)
                | _ -> None
            <| messageStream

    let MovementMessage =
        Observable.choose
        <| fun m ->
            match m with
            | MapChanged map -> currentMap <- GetMap map; None
            | Movement mv ->
                let t = time()
                latestPositionTime.[mv.Id] <- t
                Some (t, mv.Id, List.map
                          <| fun pos -> {Id=mv.Id; Coordinates=pos}
                          <| (Pathfinding.FindPath
                               <| currentMap.Data
                               <| mv.Origin
                               <| mv.Target)
                )
            | _ -> None
        <| messageStream
        |> (Observable.flatmap
            <| fun (t, id, locations) ->
                let delay =
                    TimeSpan.FromMilliseconds <|
                        match speedMap.TryGetValue id with
                          | (false, _) -> 0.0
                          | (true, speed) -> speed
                Observable.collect
                <| fun loc -> Observable.delay delay (Observable.single (t, id, loc))
                <| locations)

    Observable.merge PositionMessage MovementMessage
    |> (Observable.choose
        <| fun (t, id, loc) ->
            if t = latestPositionTime.[id]
             then Some loc
             else None)

let EntityMapStream messageStream =
    let entityStream =
        Observable.choose
        <| fun m -> match m with | New e -> Some e | _ -> None
        <| messageStream
    Observable.scanInit
    <| Map.empty
    <| fun m (e: Entity) -> m.Add(e.Id, e)
    <| entityStream

let SelfPositionStream selfId positionStream =
    Observable.filter
    <| fun (pos: Position) -> pos.Id = selfId
    <| positionStream

let EntityPositionMapStream selfId positionStream (entityMapStream: IObservable<Map<_,Entity>>) =
    let mutable selfPosition = 0s, 0s
    let TrackedEntitiesStream =
        Observable.tap
       <| fun (p: Position) ->
           if p.Id = selfId then selfPosition <- p.Coordinates
       <| positionStream
       |> (Observable.choose
            <| fun (pos: Position) ->
                let (x, y) = pos.Coordinates
                if Pathfinding.DistanceTo selfPosition (x, y) > 30s
                     then None
                     else Some pos)

       |> Observable.combineLatest entityMapStream
       |> (Observable.scanInit
           <| (Set.empty, List.empty)
           <| fun (currentSet, queue: Position list) (entities, newPos) ->
               List.fold
               <| fun (set, list) (pos: Position) ->
                   match entities.TryFind pos.Id with
                   | None -> set, pos :: list
                   | Some e ->
                       let newSet = set.Remove {Id=pos.Id;Name="";Type=EntityType.Invalid;Coordinates=0s,0s}
                       if pos.Coordinates = (-1s, -1s)
                        then newSet
                        else newSet.Add {Id=pos.Id;Name=e.Name;Type=e.Type;Coordinates=pos.Coordinates}
                       , list
               <| (currentSet, List.empty)
               <| newPos :: queue)
       |> Observable.map (fst)
    TrackedEntitiesStream

let NextJobLevelExpStream messageStream =
    Observable.choose
    <| fun m -> match m with | ExpNextJobLevel exp -> Some exp | _ -> None
    <| messageStream

let NextBaseLevelExpStream messageStream =
    Observable.choose
    <| fun m -> match m with | ExpNextBaseLevel exp -> Some exp | _ -> None
    <| messageStream

let JobLevelExpStream messageStream =
    Observable.choose
    <| fun m -> match m with | JobExp exp -> Some exp | _ -> None
    <| messageStream

let BaseLevelExpStream messageStream =
    Observable.choose
    <| fun m -> match m with | BaseExp exp -> Some exp | _ -> None
    <| messageStream
