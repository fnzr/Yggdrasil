module Yggdrasil.IO.Stream

open System
open System.IO
open System.Net.Sockets
open Yggdrasil.Utils

type OnReceiveCallback = uint16 -> byte[] -> unit
type PacketMap = Map<uint16, int>

[<Literal>]
let MAX_BUFFER_SIZE = 2056

let PacketLengthMap =
    let list = File.ReadLines ("PacketMap.txt") |> List.ofSeq
            |> List.map (fun i ->
                 let parts = i.Split(' ')
                 ToUInt16 (Hex.decode (parts.[0]) |> Array.rev) , Convert.ToInt32(parts.[1]))
    AggregatePacketMap Map.empty list

let Reader (queue: byte[]) (callback: OnReceiveCallback) =
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
            then callback packetType queue.[..(packetLength - 1)]
                 queue.[packetLength..]
            else queue
    else queue
    
let GetReader (stream: NetworkStream) (callback: OnReceiveCallback) =
    let buffer = Array.zeroCreate 256
    let rec loop queue = async {
        let newQueue =
            try
                Some(Reader 
                    (if queue = [||] || stream.DataAvailable
                    then
                        let bytesRead = stream.Read(buffer, 0, buffer.Length) 
                        Array.concat [| queue; buffer.[.. (bytesRead - 1)] |]
                    else queue)
                    callback)
            with
            | :? IOException -> stream.Close(); None        
        match newQueue with
        | Some(q) -> return! loop q 
        | None -> ()
    }
    loop
    
let Write (stream: Stream) data = stream.Write(data, 0, data.Length)
