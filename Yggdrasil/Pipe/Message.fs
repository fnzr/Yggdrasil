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

type Speed = {
    Id: Id
    Value: uint16
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
    Origin: Position
    Target: Position
    Delay: float
}
    

type Message =
    | New of Entity
    | Speed of Speed
    | Location of Location
    | Movement of Movement 

type EntityObservables = {
    Entities: IObservable<Entity>
    Locations: IObservable<Location>
}

let CreateObservables entryPoint =
    let MovementMessage =
        Observable.groupBy
        <| fun m -> m.Id
        <| (Observable.choose
            <| fun m ->
                match m with
                | Movement k -> Some k
                | Location l ->                
                    Some {Id=l.Id; Map=l.Map;Origin=l.Position; Target=l.Position;Delay=0.0}
                | _ -> None
            <| entryPoint
            |> Observable.distinctUntilChanged)
        |> Observable.map (fun i -> printfn "MovementKey: %A" i.Key; i)
        
    let NewEntity =
        Observable.choose
            <| fun m -> match m with New e -> Some e | _ -> None
            <| entryPoint
        |> Observable.distinctUntilChanged
    
    let SpeedMessage =
        Observable.groupBy
        <| fun (s: Speed) -> s.Id
        <| (Observable.choose
            <| fun m -> match m with Speed s -> Some s | _ -> None
            <| entryPoint
            |> Observable.distinctUntilChanged)
        |> Observable.map (fun i -> printfn "SpeedKey: %A" i.Key; i) 

    let CreateSteps move speed =
        printfn "==dasdsadasdasdas="
        let path = Yggdrasil.Navigation.Pathfinding.FindPath
                       <| move.Map.Data
                       <| Position.Value move.Origin
                       <| Position.Value move.Target
        let delay = TimeSpan.FromMilliseconds (float speed.Value)
        Observable.delay
        <| TimeSpan.FromMilliseconds move.Delay
        <| (Observable.collect
            <| fun pos -> Observable.delay delay
                        <| Observable.single {Id=move.Id; Map=move.Map;Position=Known pos}
            <| path)
        
    let SpeedMessage2 =
        Observable.groupBy
        <| fun (s: Speed) -> s.Id
        <| (Observable.choose
            <| fun m -> match m with Speed s -> Some s | _ -> None
            <| entryPoint
            |> Observable.distinctUntilChanged)
        |> Observable.map (fun i -> printfn "SpeedKey: %A" i.Key; i)
        
    let SpeedStream id =
        Observable.filter
        <| fun (gs: IGroupedObservable<_,_>) -> gs.Key = id
        <| SpeedMessage
        
        
    let SpeedMove2 =
        Observable.withLatestFrom
        <| fun a b -> Observable.empty
        <| SpeedMessage
        <| MovementMessage
        (*
        Observable.combineLatest SpeedMessage SpeedMessage2 
        |> Observable.filter (fun (m, s) -> m.Key = s.Key)
        |> Observable.map (fun (s, m) ->
            m.Subscribe(printfn "888888888888%A")
            s.Subscribe(printfn "99999999999%A")
            Observable.empty)
          *)  
            

        
    let EntityLocation =
        //TODO zip post-filtered grpSpeed, grpMove
        Observable.flatmap
        <| fun (a: IGroupedObservable<_,_>) ->
            let ss = Observable.map id (SpeedStream a.Key)
            Observable.flatmap
            <| fun c -> printfn "aaaaa";Observable.WithLatestFrom (a, c,Func<_,_,_>(CreateSteps))
            <| ss
            //|> Observable.map (fun i -> printfn "==%A" i; i)
            |> Observable.switch
        <| MovementMessage
         
    {
        Entities = NewEntity
        Locations = EntityLocation
    }
