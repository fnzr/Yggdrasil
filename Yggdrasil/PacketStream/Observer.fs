module Yggdrasil.PacketStream.Observer
open FSharp.Control.Reactive
open NLog
open Yggdrasil.Pipe.Message

let Logger = LogManager.GetLogger "PacketObserver"

type PacketMessage =
    | Message of Message
    | Messages of Message list
    | Skip
    | Unhandled of uint16
    
let PacketObserver () =
    let broadcaster = Subject.broadcast
    
    broadcaster.OnNext,
    Observable.choose
    <| fun message ->
        match message with
        | Message m -> Some [m]
        | Messages ms -> Some ms
        | _ -> None
    <| broadcaster
    |> Observable.flatmap (fun m -> (Observable.collect (Observable.single) m))
    |> CreateObservables
