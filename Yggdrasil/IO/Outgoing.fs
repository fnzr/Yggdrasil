module Yggdrasil.IO.Outgoing

open System
open System.IO
open NLog
open Yggdrasil.Types
let Logger = LogManager.GetLogger "Dispatcher"

let PackPosition (x: int16) y (dir: byte) =
    [|
        byte (x >>> 2);
        byte ((x <<< 6) ||| ((y >>> 4) &&& 0x3fs))
        byte ((y <<< 4)) ||| (dir &&& 0xfuy)
    |]
    
let OnlineRequest (time: unit -> int64) (stream: Stream) (request: Request) =
    let bytes =
        match request with
        | DoneLoadingMap -> BitConverter.GetBytes 0x7dus
        | RequestServerTick -> Array.concat [|
                BitConverter.GetBytes 0x0360us
                BitConverter.GetBytes (Convert.ToUInt32(time()))
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
        | Attack id -> Array.concat [|
                BitConverter.GetBytes 0x0437us
                BitConverter.GetBytes id
                [| byte ActionType.Attack |]
            |]
        | ContinuousAttack id -> Array.concat [|
                BitConverter.GetBytes 0x0437us
                BitConverter.GetBytes id
                [| byte ActionType.ContinuousAttack |]
            |]
        | Unequip index -> Array.concat [|
                BitConverter.GetBytes 0x00ab
                BitConverter.GetBytes index
            |]
        | Equip (index, location) -> Array.concat [|
                BitConverter.GetBytes 0x0998us
                BitConverter.GetBytes index
                BitConverter.GetBytes location
            |]
    Logger.Info request
    stream.Write(bytes, 0, bytes.Length)
