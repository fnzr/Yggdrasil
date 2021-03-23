module Yggdrasil.IO.Incoming.Observer

open System.Net
open System.Net.Sockets
open FSharp.Control.Reactive
open NLog
open Yggdrasil.IO.Handshake
open Yggdrasil.Navigation.Maps
open Yggdrasil.Types
open Yggdrasil.World.Types
open Yggdrasil.World.Stream
open Yggdrasil.IO

let Logger = LogManager.GetLogger "PacketObserver"
let Tracer = LogManager.GetLogger "Tracer"
let MessageStream id time messages packetSource =
    Observable.choose
    <| fun message ->
        match message with
        | Message m -> Some [m]
        | Messages ms -> Some ms
        | Unhandled pType ->
            //Logger.Warn $"Unhandled packet: {pType:X}"; None
            Tracer.Warn $"Unhandled packet: {pType:X}"; None
        | _ -> None
    <| Packets.Observer id time packetSource
    |> Observable.flatmap (Observable.collect Observable.single)
    |> Observable.startWith messages
    |> Observable.publish

let InitialMessages id (info: BasicCharacterInfo) =
    [
        (id, info.WalkSpeed) |> Speed
        [Attribute.BaseLevel, info.BaseLevel
         Attribute.JobLevel, info.JobLevel
        ]|> Parameter
    ]

let PlayerLogin credentials time =
    let loginServer = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let info = Login loginServer credentials

    let client = new TcpClient()
    client.Connect(info.ZoneServer)
    let stream = client.GetStream()
    let messages = InitialMessages info.AccountId info.CharacterInfo
    let source = Reader.Observer client (WantToConnect info)
    ({
        Id = info.AccountId
        Name = info.CharacterInfo.Name
        InitialMap = GetMap info.MapName.[0..info.MapName.Length - 5]
        Request = Outgoing.OnlineRequest stream
        PacketStream = source
        MessageStream = MessageStream info.AccountId time messages source
    } : Player)
