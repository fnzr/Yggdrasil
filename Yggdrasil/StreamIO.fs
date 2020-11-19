module Yggdrasil.StreamIO

open System
open System.IO
open System.Net.Sockets

type OnReceiveCallback = uint16 -> byte[] -> unit
type PacketMap = Map<uint16, int>

[<Literal>]
let MAX_BUFFER_SIZE = 512
let ToUInt16 data = BitConverter.ToUInt16(data, 0)

let Reader (queue: byte[]) (packetMap: PacketMap) (callback: OnReceiveCallback) =    
    if queue.Length >= 2 then
        let packetType = ToUInt16 queue
        if queue.Length > MAX_BUFFER_SIZE then
            raise (ArgumentException (
                                         sprintf "Queue length exceeded MAX_BUFFER_SIZE (%d) with packet type %X. Exiting." MAX_BUFFER_SIZE packetType
                                     ))
        else
            let packetLength = if packetMap.ContainsKey packetType
                               then packetMap.[packetType]
                               else if queue.Length >= 4
                                    then int (ToUInt16 queue.[2..])
                                    else Int32.MaxValue
            if queue.Length >= packetLength
            then callback packetType queue.[..(packetLength - 1)]
                 queue.[packetLength..]
            else queue
    else queue
    
let GetReader (stream: NetworkStream) (packetMap: PacketMap) (callback: OnReceiveCallback) =
    let buffer = Array.zeroCreate 256
    let rec loop queue =
        let newQueue =
            try
                Some(Reader 
                    (if queue = [||] || stream.DataAvailable
                    then
                        let bytesRead = stream.Read(buffer, 0, buffer.Length) 
                        Array.concat [| queue; buffer.[.. (bytesRead - 1)] |]
                    else queue)
                    packetMap callback)
            with
            | :? IOException | :? ObjectDisposedException | :? IndexOutOfRangeException -> None        
            | :? ArgumentException as e -> printfn "%s" e.Message; None
        match newQueue with
        | Some(q) -> loop q 
        | None -> ()
    loop
    
    
let GetWriter (stream: Stream) =
    fun data ->
        try
            stream.Write(data, 0, data.Length)
        with
        | :? IOException -> ()
        | :? ObjectDisposedException -> ()