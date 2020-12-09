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

type Parameters =
    {
        mutable BaseLevel: uint32
        mutable JobLevel: uint32
        mutable HP: uint32
        mutable MaxHP: uint32
        mutable SP: uint32
        mutable MaxSP: uint32
        mutable BaseExp: int64
        mutable JobExp: int64
        mutable NextBaseExp: int64
        mutable NextJobExp: int64
        mutable StatusPoints: uint32
        mutable SkillPoints: uint32
        mutable Weight: uint32
        mutable MaxWeight: uint32
        mutable Zeny: int
        mutable STRRaw: uint16 * int16
        mutable AGIRaw: uint16 * int16
        mutable VITRaw: uint16 * int16
        mutable INTRaw: uint16 * int16
        mutable DEXRaw: uint16 * int16
        mutable LUKRaw: uint16 * int16
        mutable AttackRange: uint16
        mutable AttackSpeed: uint16
        mutable Attack1: uint16
        mutable Attack2: uint16
        mutable MagicAttack1: uint16
        mutable MagicAttack2: uint16
        mutable Defense1: uint16
        mutable Defense2: uint16
        mutable MagicDefense1: uint16
        mutable MagicDefense2: uint16
        mutable Hit: int16
        mutable Flee1: int16
        mutable Flee2: int16
        mutable Critical: int16
        mutable Speed: int64
        mutable STRUpgradeCost: int
        mutable AGIUpgradeCost: int
        mutable VITUpgradeCost: int
        mutable INTUpgradeCost: int
        mutable DEXUpgradeCost: int
        mutable LUKUpgradeCost: int
    }
    static member Default = {
        BaseLevel=0u;JobLevel=0u
        HP=0u;MaxHP=0u;SP=0u;MaxSP=0u;BaseExp=0L;JobExp=0L
        NextBaseExp=0L;NextJobExp=0L;StatusPoints=0u;SkillPoints=0u
        Weight=0u;MaxWeight=0u;Zeny=0;STRRaw=(0us,0s);AGIRaw=(0us,0s)
        VITRaw=(0us,0s);INTRaw=(0us,0s);DEXRaw=(0us,0s);LUKRaw=(0us,0s)
        AttackRange=0us;AttackSpeed=0us;Attack1=0us;Attack2=0us;MagicAttack1=0us
        MagicAttack2=0us;Defense1=0us;Defense2=0us;MagicDefense1=0us
        MagicDefense2=0us;Hit=0s;Flee1=0s;Flee2=0s;Critical=0s        
        STRUpgradeCost=0;AGIUpgradeCost=0;VITUpgradeCost=0;INTUpgradeCost=0
        DEXUpgradeCost=0;LUKUpgradeCost=0
        Speed=150L //This is seems to be a constant that the server doesnt send
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