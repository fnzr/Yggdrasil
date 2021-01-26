module Yggdrasil.Types

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
    PosPart1 : byte
    PosPart2 : byte
    PosPart3 : byte
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

type RawEquipItemBase = {
    Index: int16
    Id: uint16
    Type: byte
    Location: uint32
    WearState: uint32
    RefineLevel: byte
    Card1: uint16
    Card2: uint16
    Card3: uint16
    Card4: uint16
    ExpireDate: int
    BindOnEquipType: uint16
    SpriteNumber: uint16
    OptionCount: byte
}

type EquipFlags = {
    IsIdentified: bool
    IsDamaged: bool
    PlaceEtcTab: bool
}

type RawEquipItemOption = {
    Index: int16
    Value: int16
    Param: byte
}

type RawEquipItem = {
    Base: RawEquipItemBase
    Flags: EquipFlags
    Options: RawEquipItemOption list
}

type RawSkillCast = {
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
    x: byte
    y: byte
    dir: byte
}

type Skill = {
    Id: int16
    Type: int
    Level: int16
    SpCost: int16
    AttackRange: int16
    Name: string
    Upgradable: byte
}

type StartData = {
    StartTime: int64
    X: int
    Y: int
}

type DisappearReason =
    | OutOfSight = 0uy
    | Died = 1uy
    | LoggedOut = 2uy
    | Teleport = 3uy
    | TrickDead = 4uy


type UnitMove = {
    aid: uint32
    X: int16
    Y: int16
}

type RawDamageInfo = {
    Source: uint32
    Target: uint32
    Tick: int
    SourceSpeed: int
    TargetSpeed: int
    Damage: int
    Div: int16
    Type: byte
    Damage2: int
}

type RawDamageInfo2 = {
    Source: uint32
    Target: uint32
    Tick: int
    SourceSpeed: int
    TargetSpeed: int
    Damage: int
    IsSPDamage: byte
    Div: int16
    Type: byte
    Damage2: int
}

type DamageInfo = {
    Damage: int
    Type: byte
}

type ItemDropRaw = {
    Id: int
    NameId: int16
    Type: int16
    Identified: byte
    PosX: int16
    PosY: int16
    SubX: byte
    SubY: byte
    Amount: int16
    ShowDropEffect: byte
    DropEffectMode: byte
}

type DroppedItem = {
    Id: int
    NameId: int16
    Identified: bool
    Position: (int * int)
    Amount: int16
}

type CharacterStatusRaw = {
    Points: int16; STR: byte; USTR: byte; AGI: byte; UAGI: byte; VIT: byte
    UVIT: byte; INT: byte; UINT: byte; DEX: byte; UDEX: byte; LUK: byte
    ULUK: byte; ATK: int16; ATK2: int16; MATK_MIN: int16; MATK_MAX: int16
    DEF: int16; DEF2: int16; MDEF: int16; MDEF2: int16; HIT: int16; FLEE: int16
    FLEE2: int16; CRIT: int16; ASPD: uint16; ASPD2: int16
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
    MaxHP: int
}
type Request =
    | DoneLoadingMap
    | RequestServerTick
    | RequestMove of int16 * int16
    | Action of CommandAction
    | PickUpItem of int
    | Attack of uint32
    | ContinuousAttack of uint32
    | Unequip of int16
    | Equip of int16 * uint32
