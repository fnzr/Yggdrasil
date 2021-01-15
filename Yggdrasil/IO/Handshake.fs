module Yggdrasil.IO.Handshake

open System
open System.IO
open System.Net
open System.Net.Sockets
open NLog
open Yggdrasil.Game
open Yggdrasil.Types
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
    MapName: string
    ZoneServer: IPEndPoint
    AccountId: uint32
    LoginId1: uint32
    Gender: byte
    CharId: uint32
}

let Logger = LogManager.GetLogger("IO::Handshake")
let private OtpTokenLogin () = new ReadOnlySpan<byte> (Array.concat([|BitConverter.GetBytes(0xacfus); (Array.zeroCreate 66)|]))
let private CharSelect slot = new ReadOnlySpan<byte> (Array.concat[| BitConverter.GetBytes(0x0066us); [| slot |]|])

let private RequestToConnect (credentials: LoginServerResponse) =
    new ReadOnlySpan<byte> (Array.concat([|
        BitConverter.GetBytes(0x65us)
        BitConverter.GetBytes(credentials.AccountId)
        BitConverter.GetBytes(credentials.LoginId1)
        BitConverter.GetBytes(credentials.LoginId2)
        BitConverter.GetBytes(0us)
        [| credentials.Gender |]
    |]))

let private LoginPacket username password =
    new ReadOnlySpan<byte> (Array.concat([|
        BitConverter.GetBytes(0x0064us);
        BitConverter.GetBytes(0u);
        FillBytes username 24;
        FillBytes password 24;
        BitConverter.GetBytes('0');
    |]))
    
let WantToConnect zoneInfo =
    new ReadOnlySpan<byte> (Array.concat [|
        BitConverter.GetBytes(0x0436us)
        BitConverter.GetBytes(zoneInfo.AccountId)
        BitConverter.GetBytes(zoneInfo.CharId)
        BitConverter.GetBytes(zoneInfo.LoginId1)
        BitConverter.GetBytes(1)
        [| zoneInfo.Gender |]
    |])
    
let onAuthenticationResult (game: Game)
    (result:  Result<ZoneCredentials, string>) =
    match result with
    | Ok info ->
        Logger.Info "Connected to server"
        let client = new TcpClient()
        client.Connect(info.ZoneServer)
        
        let stream = client.GetStream()
        game.Player.Dispatch <- Outgoing.Dispatch stream
        game.Player.Name <- info.CharacterName
        game.Player.Id <- info.CharId
        stream.Write (WantToConnect info)
        Async.Start <|
        async {
            try
                try                
                    let packetHandler = Incoming.OnPacketReceived game
                    return! Array.empty |> GetReader stream packetHandler
                with
                //| :? IOException ->
                  //  Logger.Error("[{accountId}] MapServer connection closed (timed out?)", info.AccountId)                
                | :? ObjectDisposedException -> ()
                | e -> Logger.Error e
            finally
                game.Connection.Status <- Event.Inactive
                ()
        }
    | Error error -> Logger.Error error
    
let EnterZone zoneInfo packetHandler =
    let client = new TcpClient()
    client.Connect(zoneInfo.ZoneServer)
        
    let stream = client.GetStream()
    //stream.ReadTimeout <- 10000
        
    stream.Write(WantToConnect zoneInfo)
    
    //let packetHandler = ZonePacketHandler publish <| Write stream
    Async.Start <| async {
        try
            return! Array.empty |> GetReader stream packetHandler
        with
        | :? IOException -> Logger.Error("[{accountId}] MapServer connection closed (timed out?)", zoneInfo.AccountId)
        | :? ObjectDisposedException -> ()
    }
    client
    
let GetCharPacketHandler (stream: Stream) characterSlot (credentials: LoginServerResponse) onReadyToEnterZone =
    let mutable name = ""
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x82dus | 0x9a0us | 0x20dus | 0x8b9us -> ()
        | 0x6bus ->
            name <- data.[115..139] |> ToString
            stream.Write(CharSelect characterSlot)            
        | 0xac5us ->
            onReadyToEnterZone <| Ok {
                AccountId = credentials.AccountId
                CharacterName = name
                MapName = ToString <| data.[6..22]
                LoginId1 = credentials.LoginId1
                Gender = credentials.Gender
                CharId = ToUInt32 <| data.[2..]                
                ZoneServer = IPEndPoint(
                                Convert.ToInt64(ToInt32 <| data.[22..]),
                                Convert.ToInt32(ToUInt16 <| data.[26..])
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
    
    stream.Write(RequestToConnect loginServerResponse)

    //account_id feedback
    if stream.Read(Span(Array.zeroCreate 4)) <> 4 then
        raise <| IOException "Invalid byte count read"     
    
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
        
    stream.Write(OtpTokenLogin())
        
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
        | 0xae3us -> stream.Write (LoginPacket credentials.Username credentials.Password)
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

let Login loginServer username password agent =
    Async.Start (Connect  {
        LoginServer = loginServer
        Username = username
        Password = password
        CharacterSlot = 0uy
    } <| onAuthenticationResult agent)