module Yggdrasil.Pipe.Location

open System
open System.Collections.Generic
open FSharp.Control.Reactive
open NLog
open Yggdrasil.Types

type EntityPosition = {
    Id: Id
    Type: EntityType
    Map: string
    Coordinates: Coordinates
    Speed: uint16
}

type Movement = {
    Delay: float
    Origin: Coordinates
    Destination: Coordinates
}

type Report =
    | New of EntityPosition
    | Disappear of DisappearReason
    | Position of Coordinates
    | Speed of uint16
    | Movement of Movement
    | MapMove of string * Coordinates
    
type PositionUpdate =
    | Update of EntityPosition
    | LostTrack of Id
    
type TrackedEntity = {
    Id: Id
    Tracker: IDisposable list
    Broadcast: Report -> unit
    RefCount: int
}

let EntityMailbox initialEntity onUpdate =
    MailboxProcessor.Start
    <| fun (inbox: MailboxProcessor<Report>) ->
        let rec loop entity = async {
            let! report = inbox.Receive()
            let optEntity =
                match report with
                | Position p -> Some {entity with Coordinates = p}
                | Speed s -> Some {entity with Speed = s}
                | _ -> None            
            return! loop <|
                match optEntity with
                | Some newEntity ->
                    onUpdate <| Update newEntity
                    newEntity
                | None -> entity
        }
        loop initialEntity

let Tracker entity origin post =
    let source = Observable.distinctUntilChanged origin
    let SpeedObserver =
        Observable.choose (fun e -> match e with | Speed s -> Some s | _ -> None) source
        |> Observable.startWith [entity.Speed]
        
    let SpeedSpan =
        SpeedObserver
        |> Observable.map (fun s -> TimeSpan.FromMilliseconds <| float s)

    let DelayedStep (path: IObservable<#seq<_>>) =        
        Observable.flatmap
        <| fun s -> Observable.map (Observable.collect (fun q -> Observable.delay s <| Observable.single q)) path
        <| SpeedSpan
        
    let MapObserver =
        Observable.choose (fun e -> match e with | MapMove (m, _) -> Some m | _ -> None) source
        |> Observable.startWith [entity.Map]
        |> Observable.map (Yggdrasil.Navigation.Maps.GetMapData)
        
    let MovementObserver =
        Observable.choose (fun e -> match e with | Movement m -> Some m | _ -> None) source
        |> Observable.combineLatest MapObserver
        |> Observable.flatmap (fun (map, moveData) ->
            let path = Yggdrasil.Navigation.Pathfinding.FindPath map moveData.Origin moveData.Destination
            Observable.delay (TimeSpan.FromMilliseconds moveData.Delay) (Observable.single path)
            )
        |> DelayedStep
        
    let PositionObserver =
        Observable.merge
            <| Observable.choose (fun e -> match e with | Position (x, y) -> Some [(x,y)] | _ -> None) source
            <| Observable.choose (fun e -> match e with | MapMove (_, p) -> Some [p] | _ -> None) source
        |> DelayedStep

    let StepObservable =
        Observable.merge PositionObserver MovementObserver
        |> Observable.switch
        
    [
     StepObservable.Subscribe(fun p -> post <| Position p)
     SpeedObserver.Subscribe(fun s -> post <| Speed s)
    ]
    
//TODO: find out when should we remove the Tracker from a unit
//    if the unit goes OutOfSight, the server doesnt send a new
//    spawn packet when it comes back, so we cant remove for that reason,
//    for example.
let RemoveTracker tracker onUpdate onLostTrack =
    List.iter (fun (d: IDisposable) -> d.Dispose()) tracker.Tracker
    onUpdate <| LostTrack tracker.Id
    onLostTrack tracker.Id
    
let CreateTracker entity onUpdate =
    let subject = Subject.broadcast
    let mailbox = EntityMailbox entity onUpdate
    {
        Id = entity.Id
        Broadcast = subject.OnNext
        Tracker = Tracker entity subject mailbox.Post
        RefCount = 1
    }

let PositionMailbox onEntityPositionUpdate onLostTrack =
    let Logger = LogManager.GetLogger "PositionMailbox"
    let mailbox = 
        MailboxProcessor.Start
        <| fun (inbox: MailboxProcessor<Id * Report>) ->
            let trackers = Dictionary<_,_>()            
            let rec loop() = async {
                let! (id, report) = inbox.Receive()
                match report with
                | New entity ->
                    let tracker = 
                        match trackers.TryGetValue entity.Id with
                        | (false, _) -> CreateTracker entity onEntityPositionUpdate
                        | (true, tracker) -> {tracker with RefCount = tracker.RefCount + 1}
                    trackers.[entity.Id] <- tracker
                | Disappear _ ->
                    let tracker = trackers.[id]
                    if tracker.RefCount = 1 then RemoveTracker tracker onEntityPositionUpdate onLostTrack
                    else trackers.[id] <- {tracker with RefCount = tracker.RefCount - 1}
                | MapMove (m, p) ->
                    onEntityPositionUpdate <| LostTrack id
                    trackers.[id].Broadcast <| MapMove (m, p)
                | msg -> trackers.[id].Broadcast msg
                    
                return! loop()
            }
            loop()
    mailbox.Error.Add (fun e -> Logger.Error e.Message)
    mailbox