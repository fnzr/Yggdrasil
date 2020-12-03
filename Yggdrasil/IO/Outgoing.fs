module Yggdrasil.IO.Outgoing

open System
open System.IO
open NLog
open Yggdrasil.Messages
let Logger = LogManager.GetCurrentClassLogger()
let Dispatch (stream: Stream) (command: Command) =
    let bytes =
        match command with
        | DoneLoadingMap -> BitConverter.GetBytes 0x7dus
        | RequestServerTick clientTick -> Array.concat [|
            BitConverter.GetBytes 0x0360us
            BitConverter.GetBytes clientTick
            |]
        //| RequestMove (x, y, d) -> [| x; y; d |]        
    stream.Write(bytes, 0, bytes.Length)
