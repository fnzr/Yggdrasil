module Yggdrasil.Types

type Id = uint32

type Attribute =
   |AttackRange=0|Weight=1|MaxWeight=2|BaseLevel=3
   |JobLevel=4|Karma=5|Manner=6|SkillPoints=7|Hit=8
   |Flee1=9|Flee2=10|MaxSP=11|SP=12
   |AttackSpeed1=13|AttackSpeed2=14|Attack1=15|Defense1=16|MagicDefense1=17
   |Attack2=18|Defense2=19|MagicDefense2=20|Critical=21
   |MagicAttack1=22|MagicAttack2=23
   |StatusPoints=24|STR=25|AGI=26|VIT=27|INT=28
   |DEX=29|LUK=30|USTR=31|UAGI=32|UVIT=33|UINT=34
   |UDEX=35|ULUK=36

type Skill = {
    Id: int16
    Type: int
    Level: int16
    SpCost: int16
    AttackRange: int16
    Name: string
    Upgradable: byte
}

type DisappearReason =
    | OutOfSight = 0uy
    | Died = 1uy
    | LoggedOut = 2uy
    | Teleport = 3uy
    | TrickDead = 4uy

type DamageInfo = {
    Damage: int
    Type: byte
}

type DroppedItem = {
    Id: int
    NameId: int16
    Identified: bool
    Position: (int * int)
    Amount: int16
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

type EntityType =
  | Player
  | NPC
  | PC
  | Monster
  | Invalid

type Request =
    | Ping
    | DoneLoadingMap
    | RequestServerTick
    | RequestMove of int16 * int16
    | Action of CommandAction
    | PickUpItem of int
    | Attack of uint32
    | ContinuousAttack of uint32
    | Unequip of int16
    | Equip of int16 * uint32
