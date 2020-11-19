module Yggdrasil.LoginService

open System
open System.Net
open System.Net.Sockets
open System.Threading
open Yggdrasil.Utils
open Yggdrasil.Messages
open Yggdrasil.StreamIO

let private OtpTokenLogin: byte[] = Array.concat([|BitConverter.GetBytes(0xacfus); (Array.zeroCreate 66)|])

let private LoginPacket (username: string) (password: string) =
            Array.concat([|
                BitConverter.GetBytes(0x0064us);
                BitConverter.GetBytes(0u);
                FillBytes username 24;
                FillBytes password 24;
                BitConverter.GetBytes('0');
            |])

let LoginSuccessParser data =
    let span = new ReadOnlySpan<byte>(data)
    let credentials = {
        AccountId = BitConverter.ToUInt32(span.Slice(8, 4))
        LoginId1 = BitConverter.ToUInt32(span.Slice(4, 4))
        LoginId2 = BitConverter.ToUInt32(span.Slice(12, 4))
        Gender = span.Slice(46, 1).[0]
        //WebAuthToken = Encoding.UTF8.GetString(span.Slice(47, 16))
    }
    let charServer = IPEndPoint(
                                   Convert.ToInt64(BitConverter.ToInt32(span.Slice(64, 4))),
                                   Convert.ToInt32(BitConverter.ToUInt16(span.Slice(68, 2)))                                       
                               )
    charServer, credentials
     
let PacketLengthMap = Map.empty.Add(0x81us, 3)

let rec Authenticate (loginServer: IPEndPoint) (username: string) (password: string) onLoginSuccess =
    let client = new TcpClient()
    client.Connect loginServer
    let stream = client.GetStream()
    
    let writer = GetWriter stream
    writer(OtpTokenLogin)
    
    let cancelSource = new CancellationTokenSource()    
    Async.Start (async {
        let shutdown = CreateShutdownFunction client cancelSource
        let packetHandler = GetPacketHandler writer shutdown loginServer username password onLoginSuccess
        (GetReader stream PacketLengthMap packetHandler) Array.empty
    }, cancelSource.Token)

and GetPacketHandler writer shutdown loginServer username password onLoginSuccess =
    fun (packetType: uint16) (data: byte[]) ->
        match packetType with
        | 0x81us ->
            match data.[2] with
            | 8uy -> printfn "Already logged in. Retrying."
                     shutdown()
                     Authenticate loginServer username password onLoginSuccess
            | _ -> printfn "Login refused. Code: %d" data.[2]
        | 0xae3us -> writer(LoginPacket username password)
        | 0xac4us ->
            shutdown()
            onLoginSuccess (LoginSuccessParser data)
        | unknown -> printfn "Unknown packet %d. Length %d!" unknown data.Length
 