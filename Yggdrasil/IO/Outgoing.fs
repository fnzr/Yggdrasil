module Yggdrasil.IO.Outgoing

open System
open NLog
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.IO.Incoming
let Logger = LogManager.GetCurrentClassLogger()
let Dispatch stream (command: Command) =
    let bytes =
        match command with
        | DoneLoadingMap -> BitConverter.GetBytes 0x7dus
        | RequestServerTick clientTick -> Array.concat [|
            BitConverter.GetBytes 0x0360us
            BitConverter.GetBytes clientTick
            |]
        //| RequestMove (x, y, d) -> [| x; y; d |] 
    Write stream bytes
