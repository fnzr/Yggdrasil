module Sunna.Main
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Net
open System.Net.Sockets
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders
open NLog
open Yggdrasil.IO
open Yggdrasil.Pipe.Location
open Yggdrasil.Pipe.Health
open Yggdrasil.Types

let Logger = LogManager.GetLogger "Sunna"

type Game =
    {
        Clock: Stopwatch
        PositionMailbox: MailboxProcessor<Id * Report>
        Positions: IObservable<PositionUpdate>
        HealthMailbox: HealthUpdate -> unit
        Health: IObservable<Map<Id, Health>> 
        Connections: Dictionary<string, Stream>
    }
    member __.Tick () = __.Clock.ElapsedMilliseconds

let CreatePlayer id map  =
    let location = {
        Id = id
        Type = EntityType.Player
        Map = map
        Coordinates = 0s, 0s
        Speed = 150us
    }
    location

    
let PlayerLogin game credentials =
    let loginServer = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let info = Handshake.Login loginServer credentials
    let map = info.MapName.[0..info.MapName.Length - 5]
    let player = CreatePlayer info.AccountId map
    
    let client = new TcpClient()
    client.Connect(info.ZoneServer)
    let stream = client.GetStream()
    (0u, Report.New player) |> game.PositionMailbox.Post
    stream.Write (Handshake.WantToConnect info)
    
    let mutable unknownPackets: IDisposable option = None 
    let incoming =
        Stream.ObservePackets stream
        |> Observable.onErrorConcat
            <| observe {
                if unknownPackets.IsSome then unknownPackets.Value.Dispose()
                yield! Observable.empty
               }
        
    let loc = Yggdrasil.PacketStream.Location.LocationStream player.Id stream game.Tick game.PositionMailbox.Post incoming
    let unit = Yggdrasil.PacketStream.Unit.UnitStream player.Map game.PositionMailbox.Post game.HealthMailbox incoming
    let none = Yggdrasil.PacketStream.Null.NullStream incoming
    unknownPackets <- Some <| 
        (Observable.zipSeq [loc; unit; none]
        |> Observable.subscribe
            (fun ps ->
                if Seq.forall (Option.isSome) ps 
                then Logger.Warn  ("Unhandled packet: {type:X}", ps.[0].Value)))
    game.Connections.[info.CharacterName] <- stream
      
let CreateGame () =
    let pos = Subject.broadcast

    let health = Subject.broadcast
    let healthObs =
        Observable.scanInit
        <| Map.empty
        <| fun m (h: HealthUpdate) ->
            match h with            
            | Update info -> m.Add (info.Id, info)
            | _ -> m
        <| Observable.distinctUntilChanged health
        
    let onLostTrack id =
        health.OnNext <| HealthUpdate.LostTrack id
    {
        Clock = Stopwatch()
        Positions = Observable.distinctUntilChanged pos
        PositionMailbox = PositionMailbox pos.OnNext onLostTrack
        Health = healthObs
        HealthMailbox = health.OnNext
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
    PlayerLogin game credentials
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