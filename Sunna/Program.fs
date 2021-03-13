module Sunna.Main
open System
open System.Diagnostics
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders
open NLog
open Yggdrasil.IO.Incoming.Observer
open Yggdrasil.Types
open Yggdrasil.World.Message
open Yggdrasil.World.Sensor
let Logger = LogManager.GetLogger "Sunna"
      
let CaptureFirstChanceExceptions () =    
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args ->
        match args.Exception with
        | :? OperationCanceledException -> () //Often fired by Observable.switch 
        | _ -> printfn "First chance exception: %s: %s"                    
                    AppDomain.CurrentDomain.FriendlyName args.Exception.Message
                    
type Action =
    | Idle
    | Walking
    
let p a = ()

type Memory =
    {
        MovedEast: bool
    }
    static member Default = {
        MovedEast = false
    }

type MemoryObservable = {
    Observable: IObservable<Memory>
    Write: Memory -> unit 
}

module Observable = 
    let combineLatest3 a b c =
        Observable.combineLatest a b
        |> Observable.combineLatest c
        |> Observable.map (fun (c, (a, b)) -> a, b, c) 

module Memory =
    let Init () =
        let subject = Subject.broadcast
        {
            Observable = Observable.startWith [Memory.Default] subject
            Write = subject.OnNext
        } 
                    
let Behavior (player: Player) (memoryObs: MemoryObservable) =
    let FuturePosition =
        Observable.choose
        <| fun m ->
            let l =
                match m with            
                | Movement l ->
                    if l.Id = player.Id then l.Target else Position.Unknown
                | Location l ->
                    if l.Id = player.Id then l.Position else Position.Unknown
                | _ -> Position.Unknown
            match l with
            | Known (x, y) -> Some (x, y)
            | Unknown -> None
        <| player.Sensor.Messages
    
    let PlayerPosition =
        Observable.choose
        <| fun (l: Location) ->
            match if l.Id = player.Id then l.Position else Position.Unknown with
            | Known (x, y) -> Some (x, y)
            | Unknown -> None
        <| player.Sensor.Locations
        
    let Action =
        Observable.map
        <| fun (futurePosition, position) ->
            if futurePosition = position
            then Action.Idle
            else Action.Walking
        <| Observable.combineLatest FuturePosition PlayerPosition
        |> Observable.distinctUntilChanged
        
    let WalkLoop =
        Observable.combineLatest3 memoryObs.Observable PlayerPosition Action
        |> (Observable.filter
           <| fun (m, _, a) -> not m.MovedEast && a = Idle)
        |> (Observable.flatmap
               <| (fun (memory, (x, y), _) ->
                    observe {
                        let east = x+5s, y
                        yield! (Observable.single <| RequestMove east)
                        memoryObs.Write {memory with MovedEast = true}
                        yield! (
                            Observable.filter
                            <| fun (p, a) -> (p = east) && a = Idle
                            <| Observable.combineLatest PlayerPosition Action
                            |> Observable.map (fun (_, _) -> RequestMove (x, y)))
                        yield! (
                            memoryObs.Write {memory with MovedEast = false}
                            Observable.empty
                        )
                    } |> Observable.delay (TimeSpan.FromMilliseconds 3000.0)))
            
    [WalkLoop]

[<EntryPoint>]
let main _ =
    CaptureFirstChanceExceptions () 
    let clock = Stopwatch()
    let time () = clock.ElapsedMilliseconds
    let credentials = ("roboco", "111111")
    
    let player = PlayerLogin credentials time
    //player.Sensor.Locations.Subscribe(printfn "---%A")
    let actions = Behavior player (Memory.Init())
    Observable.mergeSeq actions
    |> Observable.subscribe (fun s ->
        printfn "Request: %A" s
        player.Request s)
    |> ignore
    Console.ReadKey() |> ignore
    0
