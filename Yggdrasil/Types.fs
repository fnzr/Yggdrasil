module Yggdrasil.Types

open System.Collections.Generic
open System.Threading
open NLog
open NLog


type Parameter =
    |Speed=0us|Karma=3us|Manner=4us|HP=5us|MaxHP=6us|SP=7us|MaxSP=8us
    |StatusPoints=9us|BaseLevel=11us|SkillPoints=12us
    |STR=13us|AGI=14us|VIT=15us|INT=16us|DEX=17us|LUK=18us
    |Zeny=20us|Weight=24us|MaxWeight=25us|Attack1=41us|Attack2=42us
    |MagicAttack1=44us|MagicAttack2=43us|Defense1=45us|Defense2=46us
    |MagicDefense1=47us|MagicDefense2=48us|Hit=49us|Flee1=50us
    |Flee2=51us|Critical=52us|AttackSpeed=53us|JobLevel=55us
    |AttackRange=1000us|BaseExp=1us|JobExp=2us|NextBaseExp=22us
    |NextJobExp=23us|USTR=32us|UAGI=33us|UVIT=34us|UINT=35us|UDEX=36us|ULUK=37us

type IdleUnitPartial = {
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
}

type UnitRawPart1 = {
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
}

type UnitRawPart2 = {    
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

type SkillCast = {
    source: uint32
    target: uint32
    posX: int16
    posY: int16
    skillId: int16
    property: int
    delay: uint32
    disposable: byte
}

type ReqNameTitle = {
    gid: int
    groupId: int
    name: string
    title: string
}

type RequestMove = {
    x: sbyte
    y: sbyte
    dir: sbyte
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

type StartData = {
    StartTime: int64
    X: int
    Y: int
}

type WalkData = {
    StartTime: int64
    StartX: int
    StartY: int
    EndX: int
    EndY: int
}

type UnitMove = {
    aid: uint32
    x: int16
    y: int16
}

type ActionType =
    | Attack = 0uy
    | SitDown = 2uy
    | StandUp = 3uy
    | PickUpItem = 1uy
    | ContinuousAttack = 7uy
    | TouchSkill = 12uy //??
    
type CommandAction = {
    target: uint32
    action: ActionType
}

type MonsterHPInfo = {
    aid: uint32
    HP: int
    maxHP: int
}
type Command =
    | DoneLoadingMap
    | RequestServerTick
    | RequestMove of int * int
    | Action of CommandAction
