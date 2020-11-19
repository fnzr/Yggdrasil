module Yggdrasil.Utils

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open Yggdrasil.Messages

type PacketLengthMap = Map<uint16, int>
type PacketHandler = Stream -> uint16 -> byte[] -> unit

let FillBytes (data:string) size =
    Array.concat([|
        Encoding.UTF8.GetBytes(data);
        Array.zeroCreate (size - data.Length)
   |])
    
let CreateShutdownFunction (client: TcpClient) (token: CancellationTokenSource) =
    fun () ->
        printfn "Closing connection to %s" (client.Client.RemoteEndPoint.ToString())
        token.Cancel()
        client.Dispose()
        token.Dispose()
    
let ReadBytes (stream: Stream) count =
    let buffer = Array.zeroCreate count
    match stream.Read(buffer, 0, buffer.Length) = count with
    | true -> buffer
    | false ->
        printfn "Expected %d bytes" count 
        [||]
