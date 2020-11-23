module Yggdrasil.Structure

open System
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

type Unit = {
    ObjectType: byte
    AID: uint32
    GUI: uint32
    Speed: int16
    BodyState: int16
    HealthState: int16
    EffectState : int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory1: uint16
    Accessory2: uint16
    Accessory3: uint16
    HeadPalette: int16
    BodyPalette: int16
    HeadDir: int16
    Robe: uint16
    GUID: uint32
    GEmblemVer: int16
    Honor: int16
    Virtue : int
    IsPKModeOn : byte
    Gender : byte
    PosX : byte
    PosY : byte
    Direction : byte
    xSize : byte
    State : byte
    CLevel: int16
    Font: int16
    MaxHP : int
    HP : int
    IsBoss : byte
    Body: uint16
    Name: string
}

type Skill = {
    Id: int
    Type: int
    Level: byte
    SpCost: byte
    AttackRange: byte
    Name: string
    Upgradable: byte
}
    
type MoveData = {
    StartTime: uint32
    StartX: byte
    StartY: byte
    EndX: byte
    EndY: byte
    SyX: byte
    SyY: byte
}

type UpdatePartyMemberHP() =
    member val public AccountId = 0u with get, set
    member val public HP = 0 with get, set
    member val public MaxHP = 0 with get, set
    
type Message =
    | Disconnected
    | ParameterChange of string * int
    | ParameterLongChange of string * int64
    | SpawnNPC of Unit
    | SpawnPlayer of Unit
    | AddSkill of Skill
    | WeightSoftCap of int32
    | Moving of MoveData
    | PartyMemberHP of UpdatePartyMemberHP
    | Debug

type Agent = MailboxProcessor<Message>