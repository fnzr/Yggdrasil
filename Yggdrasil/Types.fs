module Yggdrasil.Types

open System.Collections.Generic
open System.Threading
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
type Command =
    | DoneLoadingMap
    | RequestServerTick
    | RequestMove of int * int
    
type AgentEvent =
    | PositionChanged
    | ConnectionStatusChanged
    | InventoryChanged
    | BattleParametersChanged
    | LevelChanged
    | SkillsChanged
    | HPSPChanged
    | MapChanged
    | DestinationChanged
    | BTStatusChanged
    | Ping
    | GoalPositionChanged
    
[<AbstractClass>]
type EventDispatcher () =
    abstract member Logger: Logger
    abstract member SetValue: byref<'T> * 'T * AgentEvent -> unit
    abstract member Dispatch: AgentEvent -> unit 
    default this.SetValue(field, value, event) =
        if not <| EqualityComparer.Default.Equals(field, value) then
            this.Logger.Debug("{event}: {value}", string event, value)
            field <- value
            this.Dispatch event

type BattleParameters() =
    inherit EventDispatcher()
    let ev = Event<_>()
    override this.Logger = LogManager.GetLogger("BattleParameters")
    override this.Dispatch e = ev.Trigger e
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    member val STRRaw = 0us, 0s with get, set
    member val AGIRaw = 0us, 0s with get, set
    member val VITRaw = 0us, 0s with get, set
    member val INTRaw = 0us, 0s with get, set
    member val DEXRaw = 0us, 0s with get, set
    member val LUKRaw = 0us, 0s with get, set
    member val AttackRange = 0us with get, set
    member val AttackSpeed = 0us with get, set
    member val Attack1 = 0us with get, set
    member val Attack2 = 0us with get, set
    member val MagicAttack1 = 0us with get, set
    member val MagicAttack2 = 0us with get, set
    member val Defense1 = 0us with get, set
    member val Defense2 = 0us with get, set
    member val MagicDefense1 = 0us with get, set
    member val MagicDefense2 = 0us with get, set
    member val Hit = 0s with get, set
    member val Flee1 = 0s with get, set
    member val Flee2 = 0s with get, set
    member val Critical = 0s with get, set
    member val Speed = 150L with get, set
    member val STRUpgradeCost = 0 with get, set
    member val AGIUpgradeCost = 0 with get, set
    member val VITUpgradeCost = 0 with get, set
    member val INTUpgradeCost = 0 with get, set
    member val DEXUpgradeCost = 0 with get, set
    member val LUKUpgradeCost = 0 with get, set
    
type Level() =
    inherit EventDispatcher()
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger e
    override this.Logger = LogManager.GetLogger("Level")
    member val BaseLevel = 0u with get, set
    member val JobLevel = 0u with get, set
    member val BaseExp = 0L with get, set
    member val JobExp = 0L with get, set
    member val NextBaseExp = 0L with get, set
    member val NextJobExp = 0L with get, set
    member val StatusPoints = 0u with get, set
    member val SkillPoints = 0u with get, set
    
type Health() =
    inherit EventDispatcher()
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger(e)
    override this.Logger = LogManager.GetLogger("HPSP")
    member val HP = 0u with get, set
    member val MaxHP = 0u with get, set
    member val SP = 0u with get, set
    member val MaxSP = 0u with get, set
        
    
type Inventory() =
    inherit EventDispatcher()
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger e
    override this.Logger = LogManager.GetLogger("Inventory")
    member val WeightSoftCap = 0 with get, set
    member val Weight = 0u with get, set
    member val MaxWeight = 0u with get, set
    member val Zeny = 0 with get, set
    
type Location (map) =
    inherit EventDispatcher()
    let ev = Event<_>()
    let mutable map: string = map
    let mutable position = 0, 0
    let mutable destination: (int * int) option = None
    
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger(e)
    override this.Logger = LogManager.GetLogger("Location")
    member this.Map
        with get() = map
        and set v = this.SetValue(&map, v, AgentEvent.MapChanged)
    member this.Destination
        with get() = destination
        and set v = this.SetValue(&destination, v, AgentEvent.DestinationChanged)
    member this.Position
        with get() = position
        and set v = this.SetValue(&position, v, AgentEvent.PositionChanged)