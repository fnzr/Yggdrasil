module Yggdrasil.IO.Handshake

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open NLog
open Yggdrasil.Utils
open Yggdrasil.IO.Stream

type LoginCredentials = {
    LoginServer: IPEndPoint
    Username: string
    Password: string
    CharacterSlot: byte
}

type LoginServerResponse = {
    CharServer: IPEndPoint
    AccountId: uint32
    LoginId1: uint32
    LoginId2: uint32
    Gender: byte
}

type ZoneCredentials = {
    CharacterName: string
    ZoneServer: IPEndPoint
    AccountId: uint32
    LoginId1: uint32
    Gender: byte
    CharId: uint32
}

let Logger = LogManager.GetCurrentClassLogger()
let private OtpTokenLogin: byte[] = Array.concat([|BitConverter.GetBytes(0xacfus); (Array.zeroCreate 66)|])
let private CharSelect (slot: byte) = Array.concat[| BitConverter.GetBytes(0x0066us); [| slot |]|]

let private RequestToConnect (credentials: LoginServerResponse) = Array.concat([|
    BitConverter.GetBytes(0x65us)
    BitConverter.GetBytes(credentials.AccountId)
    BitConverter.GetBytes(credentials.LoginId1)
    BitConverter.GetBytes(credentials.LoginId2)
    BitConverter.GetBytes(0us)
    [| credentials.Gender |]
|])

let private LoginPacket username password =
    Array.concat([|
        BitConverter.GetBytes(0x0064us);
        BitConverter.GetBytes(0u);
        FillBytes username 24;
        FillBytes password 24;
        BitConverter.GetBytes('0');
    |])
    
let WantToConnect zoneInfo =
    Array.concat [|
        BitConverter.GetBytes(0x0436us)
        BitConverter.GetBytes(zoneInfo.AccountId)
        BitConverter.GetBytes(zoneInfo.CharId)
        BitConverter.GetBytes(zoneInfo.LoginId1)
        BitConverter.GetBytes(1)
        [| zoneInfo.Gender |]
    |]
    
let EnterZone zoneInfo packetHandler =
    let client = new TcpClient()
    client.Connect(zoneInfo.ZoneServer)
        
    let stream = client.GetStream()
    //stream.ReadTimeout <- 10000
        
    Write stream <| WantToConnect zoneInfo
    
    //let packetHandler = ZonePacketHandler publish <| Write stream
    Async.Start <| async {
        try
            return! Array.empty |> GetReader stream packetHandler
        with
        | :? IOException -> Logger.Error("[{accountId}] MapServer connection closed (timed out?)", zoneInfo.AccountId)
        | :? ObjectDisposedException -> ()
    }
    client
    
let GetCharPacketHandler stream characterSlot (credentials: LoginServerResponse) onReadyToEnterZone =
    let mutable name = ""
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x82dus | 0x9a0us | 0x20dus | 0x8b9us -> ()
        | 0x6bus ->
            name <- data.[115..139] |> Encoding.UTF8.GetString
            Write stream <| CharSelect characterSlot            
        | 0xac5us ->
            let span = new ReadOnlySpan<byte>(data)
            onReadyToEnterZone <| Ok {
                AccountId = credentials.AccountId
                CharacterName = name
                LoginId1 = credentials.LoginId1
                Gender = credentials.Gender
                CharId = BitConverter.ToUInt32(span.Slice(2, 4))                
                ZoneServer = IPEndPoint(
                                Convert.ToInt64(BitConverter.ToInt32(span.Slice(22, 4))),
                                Convert.ToInt32(BitConverter.ToUInt16(span.Slice(26, 2)))
                        )
            }
            stream.Close()
        | 0x840us -> Logger.Error("Map server unavailable")
        | unknown -> Logger.Error("Unknown CharServer packet {packetType:X}", unknown)
    
let SelectCharacter slot loginServerResponse onReadyToEnterZone = async {
    use client = new TcpClient()
    client.Connect loginServerResponse.CharServer
    let stream = client.GetStream()
    stream.ReadTimeout <- 10000
    
    Write stream <| RequestToConnect loginServerResponse

    //account_id feedback
    ReadBytes stream 4 |> ignore
    
    let packetHandler = GetCharPacketHandler stream slot loginServerResponse onReadyToEnterZone
    try
        return! Array.empty |> GetReader stream packetHandler
    with
    | :? IOException -> onReadyToEnterZone <| Error "CharServer connection closed"
    | :? ObjectDisposedException -> ()
}
    
let rec Connect loginCredentials onReadyToEnterZone = async {
    use client = new TcpClient()
    client.Connect loginCredentials.LoginServer
    let stream = client.GetStream()
    stream.ReadTimeout <- 10000
        
    Write stream OtpTokenLogin
        
    let packetHandler = GetLoginPacketHandler stream loginCredentials onReadyToEnterZone
    try
        return! Array.empty |> GetReader stream packetHandler
    with
    | :? IOException -> onReadyToEnterZone <| Error "LoginServer connection closed"
    | :? ObjectDisposedException -> ()
    }
and GetLoginPacketHandler stream credentials onReadyToEnterZone =
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x81us ->
            match data.[2] with
            | 8uy -> Logger.Info("Already logged in. Retrying.")                     
                     Async.Start <| Connect credentials onReadyToEnterZone
                     stream.Close()
            | _ -> Logger.Error("Login refused. Code: {errorCode:d}", data.[2]) 
        | 0xae3us -> Write stream (LoginPacket credentials.Username credentials.Password)
        | 0xac4us ->
            let span = new ReadOnlySpan<byte>(data)                
            let response = {
                AccountId = BitConverter.ToUInt32(span.Slice(8, 4))
                LoginId1 = BitConverter.ToUInt32(span.Slice(4, 4))
                LoginId2 = BitConverter.ToUInt32(span.Slice(12, 4))
                Gender = span.Slice(46, 1).[0]
                CharServer = IPEndPoint(
                                   Convert.ToInt64(BitConverter.ToInt32(span.Slice(64, 4))),
                                   Convert.ToInt32(BitConverter.ToUInt16(span.Slice(68, 2)))                                       
                               )
            }            
            Async.Start <| SelectCharacter credentials.CharacterSlot response onReadyToEnterZone
            stream.Close()
        | unknown -> Logger.Error("Unknown LoginServer packet {packetType:X}", unknown)
