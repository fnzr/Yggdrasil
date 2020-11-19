module Yggdrasil.ZoneService

open System
open System.Net.Sockets
open System.Threading
open Yggdrasil.Messages
open Yggdrasil.Utils
open Yggdrasil.StreamIO

let private WantToConnect (accountId: uint32) (charId: int32) (loginId1:uint32) (gender: byte) = Array.concat [|
    BitConverter.GetBytes(0x0436us)
    BitConverter.GetBytes(accountId)
    BitConverter.GetBytes(charId)
    BitConverter.GetBytes(loginId1)
    BitConverter.GetBytes(1)
    [| gender |]
|]

let GetPacketHandler writer shutdown =
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0X283us | 0xB0us | 0x9e7us | 0x0ADEus -> ()
        | 0X2EBus ->
            writer(BitConverter.GetBytes 0x7dus)
            writer(Array.concat [|
                BitConverter.GetBytes 0x0360us
                BitConverter.GetBytes 1
            |])
        | 0x0081us -> printfn "Forced disconnect. Code %d" data.[2] 
        | unknown -> printfn "Unknown packet %X. Length %d!" unknown data.Length

let Connect (zoneInfo: SpawnZoneInfo) =
    let client = new TcpClient()
    client.Connect(zoneInfo.ZoneServer)
    
    let stream = client.GetStream()
    
    let writer = GetWriter stream
    writer (WantToConnect zoneInfo.AccountId zoneInfo.CharId zoneInfo.LoginId1 zoneInfo.Gender)
    
    let cancelSource = new CancellationTokenSource()
    Async.Start (async {
        let shutdown = CreateShutdownFunction client cancelSource
        let packetHandler = GetPacketHandler writer shutdown
        (GetReader stream ZonePackets.PacketLengthMap packetHandler) Array.empty
    }, cancelSource.Token)

