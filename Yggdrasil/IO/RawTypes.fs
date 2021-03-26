module Yggdrasil.IO.RawTypes

type RawEquipItemOption = {
    Index: int16
    Value: int16
    Param: byte
}

type RawAttribute =
    |Speed=0us|Karma=3us|Manner=4us|HP=5us|MaxHP=6us|SP=7us|MaxSP=8us
    |StatusPoints=9us|BaseLevel=11us|SkillPoints=12us
    |STR=13us|AGI=14us|VIT=15us|INT=16us|DEX=17us|LUK=18us
    |Zeny=20us|Weight=24us|MaxWeight=25us|Attack1=41us|Attack2=42us
    |MagicAttack1=44us|MagicAttack2=43us|Defense1=45us|Defense2=46us
    |MagicDefense1=47us|MagicDefense2=48us|Hit=49us|Flee1=50us
    |Flee2=51us|Critical=52us|AttackSpeed1=53us|JobLevel=55us
    |AttackRange=1000us|BaseExp=1us|JobExp=2us|NextBaseExp=22us
    |NextJobExp=23us|USTR=32us|UAGI=33us|UVIT=34us|UINT=35us|UDEX=36us|ULUK=37us

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

type RawItemDrop = {
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

type CharacterStatusRaw = {
    Points: int16; STR: byte; USTR: byte; AGI: byte; UAGI: byte; VIT: byte
    UVIT: byte; INT: byte; UINT: byte; DEX: byte; UDEX: byte; LUK: byte
    ULUK: byte; ATK: int16; ATK2: int16; MATK_MIN: int16; MATK_MAX: int16
    DEF: int16; DEF2: int16; MDEF: int16; MDEF2: int16; HIT: int16; FLEE: int16
    FLEE2: int16; CRIT: int16; ASPD: uint16; ASPD2: int16
}

type MonsterHPInfo = {
    aid: uint32
    HP: int
    MaxHP: int
}

type UnitMove = {
    Origin: int16 * int16
    Destination: int16 * int16
    TimeStart: int64 option
}

type StartData = {
    StartTime: int64
    X: int
    Y: int
}

type UnitMove2 = {
    Id: uint32
    X: int16
    Y: int16
}

type UnitWalking = {
    ObjectType: byte
    AID: uint32
    GID: uint32
    Speed: int16
    BodyState: int16
    HealthState: int16
    EffectState: int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory: uint16
    MoveStartTime: uint32
    Accessory2: uint16
    Accessory3: uint16
    HeadPalette: int16
    BodyPalette: int16
    HeadDir: int16
    Robe: uint16
    GUID: uint32
    GEmblemVer: int16
    Honor: int16
    Virtue: int
    isPKModeOn: byte
    Sex: byte
    PosDir1: byte
    PosDir2: byte
    PosDir3: byte
    xSize: byte
    ySize: byte
    CLevel: int16
    Font: int16
    MaxHP: int
    HP: int
    isBoss: byte
    Body: int16
    Name: string
}

type UnitIdle = {
    ObjectType: byte
    AID: uint32
    GID: uint32
    Speed: int16
    BodyState: int16
    HealthState: int16
    EffectState: int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory: uint16
    Accessory2: uint16
    Accessory3: uint16
    HeadPalette: int16
    BodyPalette: int16
    HeadDir: int16
    Robe: uint16
    GUID: uint32
    GEmblemVer: int16
    Honor: int16
    Virtue: int
    isPKModeOn: byte
    Sex: byte
    PosDir1: byte
    PosDir2: byte
    PosDir3: byte
    xSize: byte
    ySize: byte
    CLevel: int16
    Font: int16
    MaxHP: int
    HP: int
    isBoss: byte
    Body: int16
    Name: string
}

type UnitRawPart1 = {
    ObjectType: byte
    AID: uint32
    GID: uint32
    Speed: uint16
    BodyState: int16
    HealthState: int16
    EffectState: int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory: uint16
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
    Virtue: int
    isPKModeOn: byte
    Sex: byte
    PosPart1: byte
    PosPart2: byte
    PosPart3: byte
    xSize: byte
    ySize: byte
    State: byte
    CLevel: int16
    Font: int16
    MaxHP: int
    HP: int
    isBoss: byte
    Body: int16
    Name: string
}
