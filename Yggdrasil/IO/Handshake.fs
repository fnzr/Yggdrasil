module Yggdrasil.IO.Handshake

open System
open System.Net
open System.Net.Sockets
open System.Text
open NLog
open Yggdrasil.Communication
open Yggdrasil.Utils
open Yggdrasil.IO.Stream
open Yggdrasil.IO.Incoming

type LoginCredentials = {
    LoginServer: IPEndPoint
    Mailbox: AgentMailbox
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
    
let private WantToConnect zoneInfo =
    Array.concat [|
        BitConverter.GetBytes(0x0436us)
        BitConverter.GetBytes(zoneInfo.AccountId)
        BitConverter.GetBytes(zoneInfo.CharId)
        BitConverter.GetBytes(zoneInfo.LoginId1)
        BitConverter.GetBytes(1)
        [| zoneInfo.Gender |]
    |]
    
let EnterZone zoneInfo mailbox =
    let client = new TcpClient()
    client.Connect(zoneInfo.ZoneServer)
        
    let stream = client.GetStream()
    stream.ReadTimeout <- 10000
        
    Write stream <| WantToConnect zoneInfo
    
    let packetHandler = ZonePacketHandler mailbox <| Write stream
    Logger.Info("Entered Zone")
    Async.Start <| (GetReader stream packetHandler) Array.empty
    
let GetCharPacketHandler (mailbox: AgentMailbox) stream characterSlot (credentials: LoginServerResponse) =
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x82dus | 0x9a0us | 0x20dus | 0x8b9us -> ()
        | 0x6bus ->
            let name = data.[115..139] |> Encoding.UTF8.GetString
            mailbox.Post(Name <|  name.Trim('\x00'))
            Write stream <| CharSelect characterSlot            
        | 0xac5us ->
            let span = new ReadOnlySpan<byte>(data)
            let zoneInfo = {
                AccountId = credentials.AccountId
                LoginId1 = credentials.LoginId1
                Gender = credentials.Gender
                CharId = BitConverter.ToUInt32(span.Slice(2, 4))                
                ZoneServer = IPEndPoint(
                                Convert.ToInt64(BitConverter.ToInt32(span.Slice(22, 4))),
                                Convert.ToInt32(BitConverter.ToUInt16(span.Slice(26, 2)))
                        )  
            }
            EnterZone zoneInfo mailbox |> ignore
        | 0x840us -> Logger.Error("Map server unavailable")
        | unknown -> Logger.Error("Unknown CharServer packet {packetType:X}", unknown)
    
let SelectCharacter mailbox slot loginServerResponse = async {
    use client = new TcpClient()
    client.Connect loginServerResponse.CharServer
    let stream = client.GetStream()
    stream.ReadTimeout <- 10000
    
    Write stream <| RequestToConnect loginServerResponse

    //account_id feedback
    ReadBytes stream 4 |> ignore
    
    let packetHandler = GetCharPacketHandler mailbox stream slot loginServerResponse 
    return! (GetReader stream packetHandler) Array.empty
}
    
let rec Connect loginCredentials = async {
    use client = new TcpClient()
    client.Connect loginCredentials.LoginServer
    let stream = client.GetStream()
    stream.ReadTimeout <- 10000
        
    Write stream OtpTokenLogin
        
    let packetHandler = GetLoginPacketHandler stream loginCredentials
    return! (GetReader stream packetHandler) Array.empty
    }
and GetLoginPacketHandler stream credentials =
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x81us ->
            match data.[2] with
            | 8uy -> Logger.Info("Already logged in. Retrying.")
                     Async.Start <| Connect credentials
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
            credentials.Mailbox.Post(AccountId response.AccountId)
            Async.Start <| ((SelectCharacter credentials.Mailbox) credentials.CharacterSlot) response
        | unknown -> Logger.Error("Unknown LoginServer packet {packetType:X}", unknown)
