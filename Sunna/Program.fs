module Sunna.Main
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Net
open System.Net.Sockets
open System.Reactive
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders
open NLog
open Yggdrasil.IO
open Yggdrasil.Types

let Logger = LogManager.GetLogger "Sunna"

type Game =
    {
        Clock: Stopwatch
        Connections: Dictionary<string, Stream>
    }
    member __.Tick () = __.Clock.ElapsedMilliseconds
    
type PlayerConnection =
    {
        Id: Id
        Name: string        
        Stream: Stream
        Disconnect: unit -> unit
    }

let PlayerLogin (game: Game) credentials broadcast =
    let loginServer = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let info = Handshake.Login loginServer credentials
    let map = info.MapName.[0..info.MapName.Length - 5]
    
    let client = new TcpClient()
    client.Connect(info.ZoneServer)
    let stream = client.GetStream()
    
    ({
        Id = info.AccountId
        Type = EntityType.Player
        Name = info.CharacterName
    }: Yggdrasil.Pipe.Message.Entity)
    |> Yggdrasil.Pipe.Message.New
    |> Yggdrasil.PacketStream.Observer.Message
    |> broadcast

    let incoming = Stream.ObservePackets client (Handshake.WantToConnect info)
    
    //let loc = Yggdrasil.PacketStream.Location.LocationStream
      //            info.AccountId map game.Tick incoming
    let unit = Yggdrasil.PacketStream.Unit.UnitStream
                   info.AccountId map game.Tick incoming
    let none = Yggdrasil.PacketStream.Null.NullStream
                   game.Tick stream incoming
    let observers = seq [unit; none]
    
    let unknownPackets = 
        Observable.zipSeq observers 
        |> Observable.subscribe
            (fun ps ->
                if Seq.forall
                       <| fun m -> match m with | Yggdrasil.PacketStream.Observer.Unhandled _ -> true | _ -> false
                       <| ps 
                then
                    match ps.[0] with
                    | Yggdrasil.PacketStream.Observer.Unhandled t ->
                        Logger.Warn  ("Unhandled packet: {type:X}", t)
                    | _ -> ())
    
     
    let subscription =
        Observable.mergeSeq
        <| observers
        |> Observable.subscribe broadcast
    let connection = incoming.Connect()
    {
        Id = info.AccountId
        Name = info.CharacterName
        Stream = stream
        Disconnect = fun () ->
            Logger.Info $"Disconnecting: {info.CharacterName}"
            unknownPackets.Dispose()
            subscription.Dispose()
            connection.Dispose()
    }
      
let CreateGame () =
    {
        Clock = Stopwatch()
        Connections = Dictionary()
    }
    
let CaptureFirstChanceExceptions () =    
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args ->
        match args.Exception with
        | :? OperationCanceledException -> () //Often fired by Observable.switch 
        | _ -> printfn "First chance exception: %s: %s"                    
                    AppDomain.CurrentDomain.FriendlyName args.Exception.Message

[<EntryPoint>]
let main _ =
    CaptureFirstChanceExceptions () 
     
    let mutable game = CreateGame ()
    let credentials = ("roboco", "111111")
    //EntryPoint.Subscribe(printfn "%A") |> ignore
    let broadcast, observables = Yggdrasil.PacketStream.Observer.PacketObserver ()
    //observables.Entities.Subscribe(printfn "+++%A")
    observables.Locations.Subscribe(printfn "---%A")
    let conn = PlayerLogin game credentials broadcast
    
    //PathObservable.Subscribe(fun s -> s.Subscribe (printfn "path %A") |>ignore)
    //EntityObservable.Subscribe(fun s -> s.Subscribe (printfn "entity %A") |>ignore)
    //EntityPath.Subscribe(printfn "Message1: %A")
    //EntityMovement.Subscribe(fun s -> s.Subscribe (printfn "movement %A") |>ignore)
    //let map = Yggdrasil.Navigation.Maps.WalkableMap 100us
    //EntryPoint.OnNext(Speed {Id=0u; Value=1000us})
    //EntryPoint.OnNext(Speed {Id=1u; Value=500.0})
    //EntryPoint.OnNext(Location {Id=0u; Coordinates=(150s, 150s)})
    //EntryPoint.OnNext(Movement {Id=0u; Map=map; Origin=(10s,10s); Target=(20s,20s); Delay=0.0})
    //EntryPoint.OnNext(Movement {Id=1u; Map=map; Origin=(50s,50s); Target=(70s,70s); Delay=0.0})
    //Threading.Thread.Sleep 5000
    //EntryPoint.OnNext(Location {Id=0u; Map=map; Position=(80s,80s)})
    Console.ReadKey() |> ignore
    0

(*(bl->type == BL_MOB || bl->type == BL_PC)
Position tests
let sub = Subject.broadcast
    let mailbox = Yggdrasil.Pipe.Location.PositionMailbox sub.OnNext
    sub.Subscribe(printfn "%A") |> ignore
    
    mailbox.Post <| (0u, Yggdrasil.Pipe.Location.Report.New {
        Id = 1u
        Type = EntityType.Player
        Map = "prontera"
        Coordinates = (3s, 3s)
        Speed = 150us
    })
    mailbox.Post <| (1u, Yggdrasil.Pipe.Location.Report.Position (5s, 5s))
    mailbox.Post <| (1u, Yggdrasil.Pipe.Location.Report.Movement {Delay=0.0; Origin=(140s, 140s); Destination=(150s,145s)})
*)