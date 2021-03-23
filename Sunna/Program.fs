module Sunna.Main
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Diagnostics
open System.Reactive.Linq
open System.Threading
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders
open Mindmagma.Curses
open NLog
open Yggdrasil.IO.Incoming.Observer
open Yggdrasil.Navigation
open Yggdrasil.Types
open Yggdrasil.UI
open Yggdrasil.World.Stream
open Yggdrasil.World.Types
let Logger = LogManager.GetLogger "Sunna"

let BlockHandle = new ManualResetEvent(false)

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

(*
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
*)
(*
let test time =
    let entry = Subject.broadcast
    let map = Maps.WalkableMap 100us
    let obs = PositionStream time entry map
    let q = obs.Subscribe(printfn "%A")
    Async.Start <| async {
        entry.OnNext (Position {Id=1u;Coordinates=Known (5s,5s)})
        entry.OnNext (Position {Id=2u;Coordinates=Known (10s,10s);})
        entry.OnNext (Movement {Id=1u;Origin=20s,20s;Target=22s,22s;Delay=0.0})
        entry.OnNext (Movement {Id=2u;Origin=10s,10s;Target=15s,15s;Delay=0.0})
        do! Async.Sleep 1200
        entry.OnNext (Position {Id=1u;Coordinates=Known (13s,13s)})
        do! Async.Sleep 2000

        printfn "%A" d
    }
    q
*)
let AutoPackets player =
    player.MessageStream.Subscribe(
        fun m -> match m with | MapChanged _ -> player.Request DoneLoadingMap | _ -> ()) |> ignore

    let timer = new System.Timers.Timer(12000.0)
    timer.AutoReset <- true
    Observable.subscribe
        <| fun _ -> player.Request Ping
        <| timer.Elapsed
        |> ignore
    timer

let BlankSubject = Subject.broadcast
let BlankPlayer = {
    Id = 3000u
    Name = "PName"
    InitialMap = Maps.WalkableMap 1us
    Request = fun _ -> ()
    PacketStream = Observable.empty.Publish()
    MessageStream = BlankSubject
}

let testUI time =
    let subject = Subject.broadcast
    let player = {
        Id = 3000u
        Name = "PName"
        InitialMap = Maps.WalkableMap 1us
        Request = fun _ -> ()
        PacketStream = Observable.empty.Publish()
        MessageStream = subject
    }
    Async.Start <| async {
        do! Async.Sleep 1000
        subject.OnNext (MapChanged "prontera")
        subject.OnNext (Position {Id=3000u;Coordinates=(2s, 2s)})
        do! Async.Sleep 1000
        subject.OnNext (Connected true)
    }
    UI.InitUI time player

[<EntryPoint>]
let main _ =
    CaptureFirstChanceExceptions ()
    Async.Start <| async { Server.StartServer()}
    let clock = Stopwatch()
    clock.Start()
    let time () = clock.ElapsedMilliseconds
    //testUI time
    //Console.ReadKey() |> ignore
    let credentials = ("roboco", "111111")

    let player = PlayerLogin credentials time
    let timer = AutoPackets player
    Async.Start <| async {
        do! Async.Sleep 1000
        player.PacketStream.Connect() |> ignore
        timer.Start()
    }
    UI.InitUI time player
    //UI.InitUI time BlankPlayer
    BlockHandle.WaitOne() |> ignore
    0
