namespace Yggdrasil.Game

open FSharpPlus.Lens

type Attribute =
    | STR of int16 | AGI of int16 | VIT of int16
    | INT of int16 | DEX of int16 | LUK of int16

type PrimaryAttributes =
    {
        STR: int16
        AGI: int16
        VIT: int16
        INT: int16
        DEX: int16
        LUK: int16
    }
    static member Default = {STR=0s;AGI=0s;VIT=0s;INT=0s;DEX=0s;LUK=0s;}
    
type BattleParameters =
    {
        AttackRange: uint16
        AttackSpeed: uint16
        Attack1: int16
        Attack2: int16
        MagicAttack1: int16
        MagicAttack2: int16
        Defense1: int16
        Defense2: int16
        MagicDefense1: int16
        MagicDefense2: int16
        Hit: int16
        Flee1: int16
        Flee2: int16
        Critical: int16
    }
    static member Default = {AttackRange = 0us;AttackSpeed = 0us;Attack1 = 0s;Attack2 = 0s
                             MagicAttack1 = 0s;MagicAttack2 = 0s;Defense1 = 0s;Defense2 = 0s
                             MagicDefense1 = 0s;MagicDefense2 = 0s;Hit = 0s;Flee1 = 0s
                             Flee2 = 0s;Critical = 0s}

type Attributes =
    {
        Points: int16
        Primary: PrimaryAttributes
        Bonus: PrimaryAttributes
        UpgradeCost: PrimaryAttributes
    }
    
    static member Default = {Points=0s;Primary = PrimaryAttributes.Default
                             Bonus = PrimaryAttributes.Default
                             UpgradeCost = PrimaryAttributes.Default}
    
module Attributes =
    let inline _Primary f p = f p.Primary <&> fun x -> {p with Primary = x}
    let inline _Bonus f p = f p.Bonus <&> fun x -> {p with Bonus = x}
    let inline _UpgradeCost f p = f p.UpgradeCost <&> fun x -> {p with UpgradeCost = x}