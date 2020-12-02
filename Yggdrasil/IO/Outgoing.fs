module Yggdrasil.IO.Outgoing

open System
open Yggdrasil.Types
open Yggdrasil.Utils

let Dispatch stream (command: Command) =
    let write = Write stream
    let bytes =
        match command with
        | DoneLoadingMap -> BitConverter.GetBytes 0x7dus
        | RequestServerTick clientTick -> Array.concat [|
            BitConverter.GetBytes 0x0360us
            BitConverter.GetBytes clientTick
        |]
    write bytes

