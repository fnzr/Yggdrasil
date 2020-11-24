module Yggdrasil.Structure

open System
open System.Net
open System.Runtime.CompilerServices

  
type Message =
    | Disconnected
    | ParameterChange of string * int
    | ParameterLongChange of string * int64
    | SpawnNPC of Unit
    | SpawnPlayer of Unit
    | WeightSoftCap of int32
    | Debug

type Agent = MailboxProcessor<Message>