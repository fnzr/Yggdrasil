module Yggdrasil.IO.Handshake

open System
open System.IO
open System.Net
open System.Net.Sockets
open NLog
open Yggdrasil.Game
open Yggdrasil.Utils
open Yggdrasil.IO.Stream

type PlayerInfo = {
    ZoneServer: IPEndPoint
    CharacterName: string
    MapName: string
    AccountId: uint32
    LoginId1: uint32
    CharId: uint32
    Gender: byte
}

let Logger = LogManager.GetLogger("IO::Handshake")
    
let WantToConnect playerInfo =
    ReadOnlySpan<byte> (Array.concat [|
        BitConverter.GetBytes(0x0436us)
        BitConverter.GetBytes(playerInfo.AccountId)
        BitConverter.GetBytes(playerInfo.CharId)
        BitConverter.GetBytes(playerInfo.LoginId1)
        BitConverter.GetBytes(1)
        [| playerInfo.Gender |]
    |])
    
    
type LoginServerResponse = {
    CharServer: IPEndPoint
    AccountId: uint32
    LoginId1: uint32
    LoginId2: uint32
    Gender: byte
}

type CharServerResponse = {
    ZoneServer: IPEndPoint
    CharacterName: string
    MapName: string
    CharId: uint32
}
let OtpTokenLogin () = ReadOnlySpan<byte> (Array.concat([|BitConverter.GetBytes(0xacfus); (Array.zeroCreate 66)|]))
let CharSelect slot = ReadOnlySpan<byte> (Array.concat[| BitConverter.GetBytes(0x0066us); [| slot |]|])

let RequestToConnect (credentials: LoginServerResponse) =
    ReadOnlySpan<byte> (Array.concat([|
        BitConverter.GetBytes(0x65us)
        BitConverter.GetBytes(credentials.AccountId)
        BitConverter.GetBytes(credentials.LoginId1)
        BitConverter.GetBytes(credentials.LoginId2)
        BitConverter.GetBytes(0us)
        [| credentials.Gender |]
    |]))

let LoginPacket username password =
    ReadOnlySpan<byte> (Array.concat([|
        BitConverter.GetBytes(0x0064us);
        BitConverter.GetBytes(0u);
        FillBytes username 24;
        FillBytes password 24;
        BitConverter.GetBytes('0');
    |]))
    
let SelectCharacter slot loginServerResponse =
    use client = new TcpClient()
    client.Connect loginServerResponse.CharServer
    let stream = client.GetStream()
    stream.Write(RequestToConnect loginServerResponse)

    //account_id feedback
    if stream.Read(Span(Array.zeroCreate 4)) <> 4 then
        raise <| IOException "Invalid byte count read"

    let buffer = Array.zeroCreate 1024
    let rec handler name =
        let (packetType, packetData) = ReadPacket stream buffer
        let data = packetData.ToArray()
        match packetType with
        | 0x82dus | 0x9a0us | 0x20dus | 0x8b9us -> handler name
        | 0x6bus ->
            stream.Write(CharSelect slot)
            handler <| ToString data.[115..139]
        | 0xac5us ->
            {
                CharacterName = name
                MapName = ToString <| data.[6..22]
                AccountId = loginServerResponse.AccountId
                LoginId1 = loginServerResponse.LoginId1
                CharId = ToUInt32 <| data.[2..]
                Gender = loginServerResponse.Gender
                ZoneServer = IPEndPoint(
                                data.[22..] |> ToInt32 |> int64,
                                data.[26..] |> ToUInt16 |> int)
            }
        | 0x840us -> invalidOp "Map server unavailable"
        | unknown -> invalidArg $"PacketType {unknown:X}" "Unknown LoginServer packet"
    handler ""    
        
let rec Authenticate loginServer (username, password) =
    use client = new TcpClient()
    client.Connect loginServer
    let stream = client.GetStream()    
    let buffer = Array.zeroCreate 512
    let rec handler () =
        let (packetType, packetData) = ReadPacket stream buffer
        let data = packetData.ToArray()
        match packetType with
        | 0x81us ->
            match data.[2] with
            | 8uy -> Logger.Info("Already logged in. Retrying.")                     
                     Authenticate loginServer (username, password)
            | _ -> invalidArg (string data.[2]) "Login refused" 
        | 0xae3us ->
            stream.Write (LoginPacket username password)
            handler()
        | 0xac4us ->
            let span = ReadOnlySpan<byte>(data)                
            {
                AccountId = BitConverter.ToUInt32(span.Slice(8, 4))
                LoginId1 = BitConverter.ToUInt32(span.Slice(4, 4))
                LoginId2 = BitConverter.ToUInt32(span.Slice(12, 4))
                Gender = span.Slice(46, 1).[0]
                CharServer = IPEndPoint(
                                   Convert.ToInt64(BitConverter.ToInt32(span.Slice(64, 4))),
                                   Convert.ToInt32(BitConverter.ToUInt16(span.Slice(68, 2)))                                       
                               )
            }
        | unknown -> invalidArg $"PacketType {unknown:X}" "Unknown LoginServer packet"
    stream.Write(OtpTokenLogin())
    handler()

let Login loginServer credentials =
    Authenticate loginServer credentials
    |> SelectCharacter 0uy
