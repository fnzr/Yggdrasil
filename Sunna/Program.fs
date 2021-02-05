module Sunna.Main
open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Text
open FSharp.Control.Reactive
open Yggdrasil.Behavior
open Yggdrasil.Game
open Sunna.Machines
open Yggdrasil.Game.Components
open FSharp.Control.Reactive.Builders
open Yggdrasil.IO

let StartSentry (mailbox: MailboxProcessor<_>) time (info: Handshake.PlayerInfo) =
    let player = ({
        Id = info.AccountId
        Position = 0s, 0s
        Speed = 150.0
        Map = info.MapName.Trim().[0..info.MapName.Length - 5]
        Name = info.CharacterName
    }: Yggdrasil.Types.Entity)
    mailbox.Post <| ({
        Map = player.Map
        TargetId = player.Id
        Message = Yggdrasil.Types.NewPlayer player
    } : Yggdrasil.Reactive.Monitor.Report)
    let client = new TcpClient()
    client.Connect(info.ZoneServer)
    let stream = client.GetStream()
    stream.Write (Handshake.WantToConnect info)

    Async.Start <| async {
        let buffer = Array.zeroCreate 1024
        let packetReader () = Stream.ReadPacket stream buffer
        return! Incoming.PacketParser packetReader mailbox.Post player.Id time 0L player.Map
    }
(*
    Async.Start <| async {
        let monitor = Yggdrasil.Reactive.Monitor.Monitor()
        let buffer = Array.zeroCreate 1024
        let packetReader () = ReadPacket stream buffer
        let agentUpdate = Yggdrasil.Reactive.Monitor.agentHandler
        let personaUpdate = monitor.Push
        let id = player.Id
        let time = Connection.Tick
        let tickOffset = 0L
        let map = info.MapName.Substring(0, info.MapName.Length - 4)
        
        return! Incoming.PacketParser packetReader agentUpdate personaUpdate id time tickOffset map
    }*)

let StartAgent credentials initialMachineState =
    let sw = Stopwatch()
    let time = fun () -> sw.ElapsedMilliseconds
    let entityUpdate = Subject.broadcast
    entityUpdate.Subscribe(printfn "Entity: %A") |> ignore
    let monitor = Yggdrasil.Reactive.Monitor.MonitorMailbox entityUpdate.OnNext
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let info = Handshake.Login server credentials
    StartSentry monitor time info
    //printfn "Game started! %A" game
    //Propagators.SetupPropagators game |> ignore

[<EntryPoint>]
let main _ =
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args -> printfn "First change exception: %s: %s" AppDomain.CurrentDomain.FriendlyName args.Exception.Message
    StartAgent ("roboco", "111111") ()
    Console.ReadKey() |> ignore
    0
