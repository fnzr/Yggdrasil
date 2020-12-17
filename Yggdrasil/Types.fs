module Yggdrasil.Types

open System.Threading

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
    
type Goals =
    {
        mutable Position: (int * int) option
    }
    static member Default = {Position=None}
    
//this should be used in a single thread, in the behavior mailbox.
//it should be fine to make it mutable if needed...
type Agent =
    {
        Name: string
        Position: int * int
        Destination: (int * int) option
        Inventory: Inventory
        BattleParameters: BattleParameters
        Level: Level
        Skills: Skill list
        HPSP: HPSP
        Map: string
        Goals: Goals
        IsConnected: bool
        Mailbox: MailboxProcessor<StateMessage>
        Dispatcher: Command -> unit
    }
    static member Create name map mailbox dispatcher = {
        Name=name;Map=map;Mailbox=mailbox;Dispatcher=dispatcher;Position=(0,0);Destination=None;Inventory=Inventory.Default
        BattleParameters=BattleParameters.Default;Level=Level.Default
        Skills=[];HPSP=HPSP.Default;Goals=Goals.Default;IsConnected=false
    }
and State =
    {
        BehaviorMailbox: MailboxProcessor<StateMessage>
        
        //i want to remove this from here, but not sure...
        //used only (for now) on OnConnectionAccepted
        Dispatch: Command -> unit
        
        //necessary for pathfinding
        mutable MapName: string
        
        //Internal control
        mutable TickOffset: int64
        mutable WalkCancellationToken: CancellationTokenSource option
        
        //"stacking" states
        //changes are applied and posted as immutable structures to Behavior
        //these *references* are mutable, but the structures arent.
        mutable HPSP: HPSP
        mutable Level: Level
        mutable BattleParameters: BattleParameters
        mutable Inventory: Inventory       
        
    }
    static member Create dispatcher map mailbox = {
        BehaviorMailbox = mailbox; Level = Level.Default; HPSP = HPSP.Default
        Inventory=Inventory.Default;MapName = map
        Dispatch = dispatcher;
        BattleParameters = BattleParameters.Default;
        TickOffset=0L; WalkCancellationToken=None
    }
    
    member this.PostPosition position = this.BehaviorMailbox.Post <| Position position
    member this.PostDestination dest = this.BehaviorMailbox.Post <| Destination dest
    member this.PostNewSkill skill = this.BehaviorMailbox.Post <| NewSkill skill
    member this.PostInventory () = this.BehaviorMailbox.Post <| Inventory this.Inventory
    member this.PostBattleParameters () = this.BehaviorMailbox.Post <| BattleParameters this.BattleParameters
    member this.PostLevel () = this.BehaviorMailbox.Post <| Level this.Level
    member this.PostHPSP () = this.BehaviorMailbox.Post <| HPSP this.HPSP
    member this.PostMap name = this.BehaviorMailbox.Post <| Map name
    member this.PostConnectionAccepted = this.BehaviorMailbox.Post ConnectionAccepted
and
    StateMessage =
    | Position of (int * int)
    | Destination of (int * int) option
    | Inventory of Inventory
    | BattleParameters of BattleParameters
    | Level of Level
    | NewSkill of Skill
    | HPSP of HPSP
    | Map of string
    | ConnectionAccepted
    | GetState of AsyncReplyChannel<Agent>
    | Ping
