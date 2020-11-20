module Yggdrasil.Structure

open System.Net
open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type Credentials = {
    AccountId: uint32
    LoginId1: uint32
    LoginId2: uint32
    Gender: byte
}

[<IsReadOnly; Struct>]
type SpawnZoneInfo = {
    AccountId: uint32
    LoginId1: uint32
    Gender: byte
    CharId: int32
    MapName: string
    ZoneServer: IPEndPoint
}

type CharacterAttribute =
    | MaxWeight

type Message =
    | Disconnected
    | AttributeChange of CharacterAttribute * uint32
    | WeightSoftCap of int32
    | Debug

type Agent = MailboxProcessor<Message>