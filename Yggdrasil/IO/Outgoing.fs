module Yggdrasil.IO.Outgoing

open System
open System.IO
open NLog
open Yggdrasil
open Yggdrasil.Messages
let Logger = LogManager.GetCurrentClassLogger()

let PackPosition (x, y, dir) =
    [|
        x >>> 2;
        (x <<< 6) ||| ((y >>> 4) &&& 0x3fuy)
        (y <<< 4) ||| (dir &&& 0xfuy)
    |]
    
let Dispatch (stream: Stream) (command: Command) =
    let bytes =
        match command with
        | DoneLoadingMap -> BitConverter.GetBytes 0x7dus
        | RequestServerTick -> Array.concat [|
            BitConverter.GetBytes 0x0360us
            BitConverter.GetBytes (Scheduling.GetCurrentTick())
            |]
        | RequestMove (x, y, d) -> Array.concat [|
            BitConverter.GetBytes 0x035fus
            PackPosition (x, y, d)
            |]
    stream.Write(bytes, 0, bytes.Length)
