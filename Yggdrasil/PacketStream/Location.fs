module Yggdrasil.PacketStream.Location
open System
open FSharp.Control.Reactive
open Yggdrasil.IO
open Yggdrasil.Utils
open Yggdrasil.Types
open Yggdrasil.IO.Decoder
open Yggdrasil.Pipe.Location

let LocationStream playerId stream tick mailbox incoming =
    let mutable tickOffset = 0L
    Observable.map(fun (pType, (pData: ReadOnlyMemory<_>)) ->
        let mutable skipped = None
        let data = pData.ToArray()        
        match pType with
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]            
            tickOffset <- int64 (ToUInt32 data.[2..])
            (playerId, Position (x, y)) |> mailbox
        | 0x0091us ->
            let position = (data.[18..] |> ToInt16,
                            data.[20..] |> ToInt16)
            let map = (let gatFile = ToString data.[..17]
               gatFile.Substring(0, gatFile.Length - 4))            
            (playerId, MapMove (map, position)) |> mailbox
            Outgoing.OnlineRequest tick stream DoneLoadingMap
        | 0x0088us -> 
            let info = MakeRecord<UnitMove2> data.[2..]
            (info.Id, Position (info.X, info.Y)) |> mailbox
        | 0x0080us ->            
            let reason = Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason
            (ToUInt32 data.[2..], Disappear reason) |> mailbox
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[2..]) + tickOffset |> float
            (playerId, Movement {
                Origin = (x0, y0)
                Destination = (x1, y1)
                Delay = if delay < 0.0 then 0.0 else delay
            }) |> mailbox
        | 0x0086us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[12..]) + tickOffset |> float
            (ToUInt32 data.[2..], Movement {
                Origin = (x0, y0)
                Destination = (x1, y1)
                Delay = if delay < 0.0 then 0.0 else delay
            }) |> mailbox
        | 0x00b0us ->
            if (data.[2..] |> ToParameter) = Parameter.Speed then
                (playerId , Speed (ToUInt16 data.[4..])) |> mailbox
        | _ -> skipped <- Some pType
        skipped
    ) incoming
