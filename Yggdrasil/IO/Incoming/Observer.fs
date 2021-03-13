module Yggdrasil.IO.Incoming.Observer

open System
open System.Net
open System.Net.Sockets
open System.Reactive.Subjects
open FSharp.Control.Reactive
open NLog
open Yggdrasil.Types
open Yggdrasil.World.Sensor
open Yggdrasil.World.Message
open Yggdrasil.IO

let Logger = LogManager.GetLogger "PacketObserver"

type PlayerInitialData = {
    Id: Id
    Name: string
    Map: string
    Request: Request -> unit
    PacketSource: IConnectableObservable<uint16 * ReadOnlyMemory<byte>>
}

let ReportUnknownPacket packetObservers =
    let isUnhandled message =
        match message with
        | Unhandled _ -> true
        | _ -> false
            
    Observable.zipSeq packetObservers
    |> Observable.subscribe
        (fun ps ->
                if Seq.forall isUnhandled ps
                then match ps.[0] with
                     | Unhandled t -> Logger.Warn ("Unhandled packet: {type:X}", t)
                     | _ -> ())

let ReceivePackets playerData time postMessage =
    let observers = seq [
        Unit.UnitStream playerData.Id playerData.Map time playerData.PacketSource
        Null.NullStream playerData.Request playerData.PacketSource
    ]
    let unknown = ReportUnknownPacket observers
    let cbSubscription = Observable.mergeSeq observers
                         |> Observable.subscribe postMessage
    let sourceConnection = playerData.PacketSource.Connect()
    
    [unknown; cbSubscription; sourceConnection]
    
let CreateSensor () =
    let broadcaster = Subject.broadcast
    let sensor = 
        Observable.choose
        <| fun message ->
            match message with
            | Message m -> Some [m]
            | Messages ms -> Some ms
            | _ -> None
        <| broadcaster
        |> Observable.flatmap (Observable.collect Observable.single)
        |> CreateObservableGraph
    broadcaster.OnNext, sensor
    
let CreatePlayer player time =
    let (postMessage, sensor) = CreateSensor ()
    ({
        Id = player.Id
        Type = EntityType.Player
        Name = player.Name
    }: Entity)
    |> New
    |> Message
    |> postMessage
    
    {
        Id = player.Id
        Request = player.Request
        Sensor = sensor
        Subscriptions = ReceivePackets player time postMessage
    }
    
let PlayerLogin credentials time =
    let loginServer = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let info = Handshake.Login loginServer credentials
    
    let client = new TcpClient()
    client.Connect(info.ZoneServer)
    let stream = client.GetStream()
    
    CreatePlayer {
        Id = info.AccountId
        Name = info.CharacterName
        Request = Outgoing.OnlineRequest time stream
        Map = info.MapName.[0..info.MapName.Length - 5]
        PacketSource = Stream.ObservePackets client (Handshake.WantToConnect info)
    } time