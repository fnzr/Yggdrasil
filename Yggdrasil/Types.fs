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
    
[<AbstractClass>]
type EventDispatcher () =
    let ev = new Event<_>()
    abstract member Logger: Logger
    abstract member SetValue: byref<'T> * 'T * AgentEvent -> unit
    abstract member OnEventDispatched: IEvent<AgentEvent>
    
    default this.OnEventDispatched = ev.Publish
    
    default this.SetValue(field, value, event) =
        if not <| EqualityComparer.Default.Equals(field, value) then
            this.Logger.Debug("{event}", string event)
            field <- value
            ev.Trigger event

type BattleParameters =
    {
        //BaseLevel: uint32
        //JobLevel: uint32
        //HP: uint32
        //MaxHP: uint32
        //SP: uint32
        //MaxSP: uint32
        //BaseExp: int64
        //JobExp: int64
        //NextBaseExp: int64
        //NextJobExp: int64
        //StatusPoints: uint32
        //SkillPoints: uint32        
        STRRaw: uint16 * int16
        AGIRaw: uint16 * int16
        VITRaw: uint16 * int16
        INTRaw: uint16 * int16
        DEXRaw: uint16 * int16
        LUKRaw: uint16 * int16
        AttackRange: uint16
        AttackSpeed: uint16
        Attack1: uint16
        Attack2: uint16
        MagicAttack1: uint16
        MagicAttack2: uint16
        Defense1: uint16
        Defense2: uint16
        MagicDefense1: uint16
        MagicDefense2: uint16
        Hit: int16
        Flee1: int16
        Flee2: int16
        Critical: int16
        Speed: int64
        STRUpgradeCost: int
        AGIUpgradeCost: int
        VITUpgradeCost: int
        INTUpgradeCost: int
        DEXUpgradeCost: int
        LUKUpgradeCost: int
    }
    static member Default = {
        STRRaw=(0us,0s);AGIRaw=(0us,0s)
        VITRaw=(0us,0s);INTRaw=(0us,0s);DEXRaw=(0us,0s);LUKRaw=(0us,0s)
        AttackRange=0us;AttackSpeed=0us;Attack1=0us;Attack2=0us;MagicAttack1=0us
        MagicAttack2=0us;Defense1=0us;Defense2=0us;MagicDefense1=0us
        MagicDefense2=0us;Hit=0s;Flee1=0s;Flee2=0s;Critical=0s        
        STRUpgradeCost=0;AGIUpgradeCost=0;VITUpgradeCost=0;INTUpgradeCost=0
        DEXUpgradeCost=0;LUKUpgradeCost=0
        Speed=150L //This is seems to be a constant that the server doesnt send
    }
    
type Level =
    {
        BaseLevel: uint32
        JobLevel: uint32
        BaseExp: int64
        JobExp: int64
        NextBaseExp: int64
        NextJobExp: int64
        StatusPoints: uint32
        SkillPoints: uint32
    }
    
    static member Default = {
        BaseLevel=0u;JobLevel=0u;BaseExp=0L;JobExp=0L
        NextBaseExp=0L;NextJobExp=0L;StatusPoints=0u;SkillPoints=0u
    }
    
type HPSP =
    {
        HP: uint32
        MaxHP: uint32
        SP: uint32
        MaxSP: uint32
    }    
    static member Default = {HP=0u;MaxHP=0u;SP=0u;MaxSP=0u;}
    
type Inventory =
    {
        WeightSoftCap: int
        Weight: uint32
        MaxWeight: uint32
        Zeny: int
    }
    
    static member Default = {WeightSoftCap=0;Weight=0u;MaxWeight=0u;Zeny=0}
    
type Location (map) =
    inherit EventDispatcher()
    let mutable map: string = map
    let mutable position = 0, 0
    let mutable destination: (int * int) option = None
    override this.Logger = LogManager.GetLogger("Agent")
    member this.Map
        with get() = map
        and set v = this.SetValue(&map, v, AgentEvent.MapChanged)
    member this.Destination
        with get() = destination
        and set v = this.SetValue(&destination, v, AgentEvent.DestinationChanged)
    member this.Position
        with get() = position
        and set v = this.SetValue(&position, v, AgentEvent.PositionChanged)