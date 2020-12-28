module Yggdrasil.IO.Outgoing

open System
open System.IO
open NLog
open Yggdrasil.Types
let Logger = LogManager.GetLogger("Dispatcher")

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
            BitConverter.GetBytes (Convert.ToUInt32(Handshake.GetCurrentTick()))
            |]
        | RequestMove (x, y) -> Array.concat [|
            BitConverter.GetBytes 0x035fus
            PackPosition (Convert.ToByte x, Convert.ToByte y, 1uy)
            |]
    Logger.Info ("{command}", command)
    stream.Write(bytes, 0, bytes.Length)
