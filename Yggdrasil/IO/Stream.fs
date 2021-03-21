module Yggdrasil.IO.Stream

open System
open System.IO
open System.Net.Sockets
open System.Reactive.Concurrency
open FSharp.Control.Reactive
open FSharpPlus.Control
open NLog
open Yggdrasil.IO.Decoder
open FSharp.Control.Reactive.Builders

type OnReceivePacket = uint16 * ReadOnlyMemory<byte> -> unit
type PacketMap = Map<uint16, int>

let Logger = LogManager.GetLogger "Stream"

type PacketLength =
    | Fixed of int
    | Dynamic

module Hex =
    let FromHexDigit c =
                if c >= '0' && c <= '9' then int c - int '0'
                elif c >= 'A' && c <= 'F' then (int c - int 'A') + 10
                elif c >= 'a' && c <= 'f' then (int c - int 'a') + 10
                else raise <| ArgumentException()
    let Decode (s:string) =
                match s with
                | null -> nullArg "s"
                | _ when s.Length = 0 -> Array.empty
                | _ ->
                    let mutable len = s.Length
                    let mutable i = 0
                    if len >= 2 && s.[0] = '0' && (s.[1] = 'x' || s.[1] = 'X') then do
                        len <- len - 2
                        i <- i + 2
                    if len % 2 <> 0 then invalidArg "s" "Invalid hex format"
                    else
                        let buf = Array.zeroCreate (len / 2)
                        let mutable n = 0
                        while i < s.Length do
                            buf.[n] <- byte (((FromHexDigit s.[i]) <<< 4) ||| (FromHexDigit s.[i + 1]))
                            i <- i + 2
                            n <- n + 1
                        buf

let PacketLengthMap =
    File.ReadLines ("PacketMap.txt")
    |> Seq.map (fun i ->
        let parts = i.Split(' ')
        ToUInt16 (Hex.Decode (parts.[0]) |> Array.rev), int parts.[1])
    |> Seq.fold (fun (m: Map<_,_>) (_type, length) -> m.Add(_type, length)) Map.empty

let ParsePacketHeader header =
    let pType = ToUInt16 header
    match PacketLengthMap.TryFind pType  with
    | Some -1 -> pType, Dynamic
    | Some len -> pType, Fixed len
    | None -> invalidArg $"{pType:X}" "Unmapped packet"

let ReadPacket (stream: Stream) buffer =
    let read = stream.Read(buffer, 0, 2)
    if read = 0 then None
    else
        let (pType, pLength) = ParsePacketHeader buffer
        let len, offset = match pLength with
                          | Fixed l -> l - 2, 2
                          | Dynamic ->
                              stream.Read(buffer, 2, 2) |> ignore
                              int (buffer.[2..] |> ToUInt16) - 4, 4
        if len > buffer.Length then
            Logger.Error $"Packet {pType:X} bigger than buffer length"
        stream.Read(buffer, offset, len) |> ignore
        Some (pType, ReadOnlyMemory (buffer.[..len + offset - 1]))

let Observer (client: TcpClient) wantToConnect =
    Observable.using
    <| client.GetStream
    <| fun stream ->
        stream.Write(wantToConnect, 0, wantToConnect.Length)
        let buffer = Array.zeroCreate 1024
        Observable.repeatWhile
        <| fun () -> stream.CanRead
        <| observe {
            yield! match ReadPacket stream buffer with
                    | Some p -> Observable.single p
                    | None -> Observable.empty
        }
        |> Observable.onErrorConcat (observe {
                                        yield! Observable.empty
                                     })
    |> Observable.subscribeOn NewThreadScheduler.Default
    |> Observable.publish
