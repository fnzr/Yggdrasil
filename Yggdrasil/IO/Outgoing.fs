module Yggdrasil.IO.Outgoing

open System
open System.IO
open NLog
open Yggdrasil
open Yggdrasil.Types
let Logger = LogManager.GetLogger("Dispatcher")

let PackPosition (x: int) y (dir: byte) =
    [|
        byte (x >>> 2);
        byte ((x <<< 6) ||| ((y >>> 4) &&& 0x3f))
        byte ((y <<< 4)) ||| (dir &&& 0xfuy)
    |]
    
let Dispatch (stream: Stream) (command: Command) =
    let bytes =
        match command with
        | DoneLoadingMap -> BitConverter.GetBytes 0x7dus
        | RequestServerTick -> Array.concat [|
                BitConverter.GetBytes 0x0360us
                BitConverter.GetBytes (Convert.ToUInt32(Game.Connection.Tick))
            |]
        | RequestMove (x, y) -> Array.concat [|
                BitConverter.GetBytes 0x035fus
                PackPosition x y 1uy
            |]
        | Action c -> Array.concat [|
                BitConverter.GetBytes 0x0437us
                BitConverter.GetBytes c.target
                [|byte c.action|]
            |]
        | PickUpItem id -> Array.concat [|
                BitConverter.GetBytes 0x0362us
                BitConverter.GetBytes id
        |]
    Logger.Info ("{command}", command)
    stream.Write(bytes, 0, bytes.Length)
