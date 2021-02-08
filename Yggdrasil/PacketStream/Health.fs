module Yggdrasil.PacketStream.Health

open System

let HealthStream incoming =
    Observable.map(fun (pType, pData: ReadOnlyMemory<_>) ->
        let data = pData.ToArray()
        let mutable skipped = None
        match pType with
        | _ -> skipped <- Some pType
        skipped
    )