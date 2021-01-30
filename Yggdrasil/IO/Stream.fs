module Yggdrasil.IO.Stream

open System
open System.IO
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
    
let ReadPacket (stream: Stream) buffer =
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
    pType, ReadOnlyMemory (buffer.[..len + offset - 1])

let ObservePackets (stream: Stream) =
    let buffer = Array.zeroCreate 1024
    let rec loop () =
        observe {
            yield ReadPacket stream buffer
            yield! loop ()
        }
    loop ()
