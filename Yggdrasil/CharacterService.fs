module Yggdrasil.CharacterService

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Runtime.CompilerServices
open System.Text
open System.Threading
open Yggdrasil.Messages
open Yggdrasil.Utils
open Yggdrasil.StreamIO
let PacketLengthMap = Map.empty
                        .Add(0x8480us, 4) 
                        .Add(0x8b9us, 12)
                        .Add(0xac5us, 156)
                        .Add(0x9a0us, 6)

let private RequestToConnect (credentials: Credentials) = Array.concat([|
    BitConverter.GetBytes(0x65us)
    BitConverter.GetBytes(credentials.AccountId)
    BitConverter.GetBytes(credentials.LoginId1)
    BitConverter.GetBytes(credentials.LoginId2)
    BitConverter.GetBytes(0us)
    [| credentials.Gender |]
|])

let private CharSelect (slot: byte) = Array.concat[|    
    BitConverter.GetBytes(0x0066us)
    [| slot |]
|]

let ZoneInfoParser data accountId loginId1 gender =
    let span = new ReadOnlySpan<byte>(data)
    {
        AccountId = accountId
        LoginId1 = loginId1
        Gender = gender
        CharId = BitConverter.ToInt32(span.Slice(2, 4))
        MapName =  Encoding.UTF8.GetString(span.Slice(6, 16))
        ZoneServer = IPEndPoint(
                        Convert.ToInt64(BitConverter.ToInt32(span.Slice(22, 4))),
                        Convert.ToInt32(BitConverter.ToUInt16(span.Slice(26, 2)))
        )
    }

let GetPacketHandler writer shutdown accountId loginId1 gender characterSlot onCharacterSelected =
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x82dus | 0x9a0us | 0x20dus | 0x8b9us -> ()
        | 0x6bus -> writer(CharSelect characterSlot)
        | 0xac5us ->
            shutdown()
            onCharacterSelected (ZoneInfoParser data accountId loginId1 gender)            
        | unknown -> printfn "Unknown packet %X. Length %d!" unknown data.Length
    
let SelectCharacter charServer (credentials: Credentials) characterSlot onCharacterSelected =
    let client = new TcpClient()
    client.Connect charServer
    let stream = client.GetStream()   
 
    let writer = GetWriter stream
    writer(RequestToConnect credentials)

    //account_id feedback
    ReadBytes stream 4 |> ignore
    
    let cancelSource = new CancellationTokenSource()
    Async.Start (async {
        let shutdown = CreateShutdownFunction client cancelSource
        let packetHandler = GetPacketHandler writer shutdown credentials.AccountId credentials.LoginId1 credentials.Gender characterSlot onCharacterSelected
        (GetReader stream PacketLengthMap packetHandler) Array.empty
    }, cancelSource.Token)