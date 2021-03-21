module Yggdrasil.IO.Incoming.Observer

open System.Net
open System.Net.Sockets
open FSharp.Control.Reactive
open NLog
open Yggdrasil.Navigation.Maps
open Yggdrasil.World.Sensor
open Yggdrasil.World.Message
open Yggdrasil.IO

let Logger = LogManager.GetLogger "PacketObserver"
let MessageStream id time packetSource =
    Observable.choose
    <| fun message ->
        match message with
        | Message m -> Some [m]
        | Messages ms -> Some ms
        //| Unhandled pType -> Logger.Warn $"Unhandled packet: {pType:X}"; None
        | _ -> None
    <| Packets.Observer id time packetSource
    |> Observable.flatmap (Observable.collect Observable.single)

let PlayerLogin credentials time =
    let loginServer = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let info = Handshake.Login loginServer credentials

    let client = new TcpClient()
    client.Connect(info.ZoneServer)
    let stream = client.GetStream()
    let source = Stream.Observer client (Handshake.WantToConnect info)
    ({
        Id = info.AccountId
        Name = info.CharacterName
        InitialMap = GetMap info.MapName.[0..info.MapName.Length - 5]
        Request = Outgoing.OnlineRequest stream
        PacketStream = source
        MessageStream = MessageStream info.AccountId time source
    } : Player)
