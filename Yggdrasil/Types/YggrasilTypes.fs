module Yggdrasil.YggrasilTypes

type Agent = {
    mutable AccountId: uint32
    mutable CharacterName: string
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
    mutable Speed: uint16
    mutable STRUpgradeCost: int
    mutable AGIUpgradeCost: int
    mutable VITUpgradeCost: int
    mutable INTUpgradeCost: int
    mutable DEXUpgradeCost: int
    mutable LUKUpgradeCost: int
}
