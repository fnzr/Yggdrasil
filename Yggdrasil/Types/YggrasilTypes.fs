module Yggdrasil.YggrasilTypes

type SecondaryStatus = {
    mutable AttackRange: int
    mutable AttackSpeed: int
    mutable Attack1: int
    mutable Attack2: int
    mutable MagicAttack1: int
    mutable MagicAttack2: int
    mutable Defense1: int
    mutable Defense2: int
    mutable MagicDefense1: int
    mutable MagicDefense2: int
    mutable Hit: int
    mutable Flee1: int
    mutable Flee2: int
    mutable Critical: int
    mutable Speed: int
}

type ExtraStatus = {
    mutable MaxWeight: int
    mutable Karma: int
    mutable Manner: int    
}

type PrimaryStatus = {
    mutable BaseExp: int64
    mutable JobExp: int64
    mutable NextBaseExp: int64
    mutable NextJobExp: int64
    mutable StatusPoints: int
    mutable SkillPoints: int
    mutable Weight: int
    mutable Zeny: int    
}

type Attributes = {
    mutable STR: int
    mutable AGI: int
    mutable VIT: int
    mutable INT: int
    mutable DEX: int
    mutable LUK: int
}

type CharacterStatus = {
    AccountId: uint32
    mutable BaseLevel: int
    mutable JobLevel: int
    mutable HP: int
    mutable MaxHP: int
    mutable SP: int
    mutable MaxSP: int
}

type Agent = Attributes * CharacterStatus * PrimaryStatus * SecondaryStatus * ExtraStatus

type Event =
    | StatusChanged of Agent
    | HealthChanged of Agent