module Yggdrasil.IO.Stream

open System
open System.IO
open System.Net.Sockets
open FSharpPlus.Control
open NLog
open Yggdrasil.Utils
open FSharp.Control.Reactive.Builders

type OnReceivePacket = uint16 * ReadOnlyMemory<byte> -> unit
type PacketMap = Map<uint16, int>

let Logger = LogManager.GetLogger "Stream"
[<Literal>]
let MAX_BUFFER_SIZE = 2056

type PacketLength =
    | Fixed of int
    | Dynamic

let PacketLengthMap =    
    File.ReadLines ("PacketMap.txt")
    |> Seq.map (fun i ->
        let parts = i.Split(' ')
        ToUInt16 (Hex.decode (parts.[0]) |> Array.rev) , Convert.ToInt32(parts.[1]))
    |> Seq.fold (fun (m: Map<_,_>) (_type, length) -> m.Add(_type, length)) Map.empty
    
let ParsePacketHeader header =
    let pType = ToUInt16 header
    match PacketLengthMap.TryFind pType  with
    | Some -1 -> pType, Dynamic
    | Some len -> pType, Fixed len
    | None -> invalidArg ($"{pType:X}") "Unmapped packet"
    
let ReadPackets (stream: Stream) =
    let buffer = Array.zeroCreate 1024
    let rec loop () =
        observe {
            stream.Read(buffer, 0, 2) |> ignore
            let (pType, pLength) = ParsePacketHeader buffer
            let len, offset = match pLength with
                              | Fixed l -> l - 2, 2
                              | Dynamic ->
                                  stream.Read(buffer, 2, 2) |> ignore
                                  int (buffer.[2..] |> ToUInt16) - 4, 4
            if len > buffer.Length then
                Logger.Error ("Packet {type} bigger than buffer length", pType)
            stream.Read(buffer, offset, len) |> ignore
            yield (pType, ReadOnlyMemory (buffer.[..len + offset - 1]))
            yield! loop ()
        }
    loop ()
    
//Deprecated
//TODO: rewrite Handshake to not use this
let Reader (queue: byte[]) (callback: OnReceivePacket) =
    if queue.Length >= 2 then
        let packetType = ToUInt16 queue
        if packetType = 0us then queue.[2..] else            
        if queue.Length > MAX_BUFFER_SIZE then
            raise (ArgumentException (
                                         sprintf "Queue length exceeded MAX_BUFFER_SIZE (%d) with packet type %X. Exiting." MAX_BUFFER_SIZE packetType
                                     ))
        else
            let packetLength = if PacketLengthMap.ContainsKey packetType
                               then
                                   match PacketLengthMap.[packetType] with
                                   | -1 -> if queue.Length >= 4
                                            then int (ToUInt16 queue.[2..])
                                            else Int32.MaxValue
                                   | len -> len
                               else raise (ArgumentException (sprintf "Unmapped packet %X. bytes in queue: %d" packetType queue.Length))
            if queue.Length >= packetLength
            then callback (packetType, ReadOnlyMemory queue.[..(packetLength - 1)])
                 queue.[packetLength..]
            else queue
    else queue
    
//Deprecated
//TODO: rewrite Handshake to not use this
let GetReader (stream: NetworkStream) (callback: OnReceivePacket) =
    let buffer = Array.zeroCreate 256
    let rec loop queue = async {
        let newQueue =
            if stream.DataAvailable || queue = [||] then
                let bytesRead = stream.Read(buffer, 0, buffer.Length)
                if bytesRead = 0 then raise <| IOException "Stream closed"
                else Array.concat [| queue; buffer.[.. (bytesRead - 1)] |]
            else queue
        if newQueue = [||] then return ()
        else return! loop <| Reader newQueue callback
    }
    loop
