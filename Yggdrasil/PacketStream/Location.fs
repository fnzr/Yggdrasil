module Yggdrasil.PacketStream.Location
open System
open FSharp.Control.Reactive
open Yggdrasil.Types
open Yggdrasil.IO.Decoder
open Yggdrasil.Pipe.Message
open Yggdrasil.PacketStream.Observer

let LocationStream playerId startMap tick =
    let mutable tickOffset = 0L
    let mutable map = Yggdrasil.Navigation.Maps.GetMap startMap
    Observable.map(fun (pType, (pData: ReadOnlyMemory<_>)) ->
        let data = pData.ToArray()        
        match pType with
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]            
            tickOffset <- int64 (ToUInt32 data.[2..])
            {
                Id = playerId
                Map = map
                Position = Known (x, y)
            } |> Location |> Message
        | 0x0091us ->
            map <- Yggdrasil.Navigation.Maps.GetMap
                       (let gatFile = ToString data.[..17]
                    gatFile.Substring(0, gatFile.Length - 4))
            {
                Id = playerId
                Map = map
                Position = Known (data.[18..] |> ToInt16, data.[20..] |> ToInt16)
            } |> Location |> Message
        | 0x0088us -> 
            let info = MakeRecord<UnitMove2> data.[2..]
            {
                Id = info.Id
                Map = map
                Position = Known (info.X, info.Y)
            } |> Location |> Message
        | 0x0080us ->
            //TODO handle disappear reason
            let reason = Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason
            {
                Id = ToUInt32 data.[2..]
                Map = map
                Position = Unknown
            } |> Location |> Message
            //(ToUInt32 data.[2..], Disappear reason) |> mailbox
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[2..]) + tickOffset |> float
            {
                Id = playerId
                Map = map
                Origin = Known (x0, y0)
                Target = Known (x1, y1)
                Delay = if delay < 0.0 then 0.0 else delay
            } |> Movement |> Message
        | 0x0086us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[12..]) + tickOffset |> float
            {
                Id = ToUInt32 data.[2..]
                Map = map
                Origin = Known (x0, y0)
                Target = Known (x1, y1)
                Delay = if delay < 0.0 then 0.0 else delay
            } |> Movement |> Message
        | 0x00b0us ->            
            if (data.[2..] |> ToParameter) = Parameter.Speed then
                {
                    Id = playerId
                    Value = ToUInt16 data.[4..]
                } |> Speed |> Message
            else Skip
        | _ -> Unhandled pType
    )
