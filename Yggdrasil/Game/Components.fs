namespace Yggdrasil.Game

open FSharpPlus.Lens
open Yggdrasil.Types

module Components = 
    type Level() =
        member val BaseLevel = 0u with get, set
        member val JobLevel = 0u with get, set
        member val BaseExp = 0L with get, set
        member val JobExp = 0L with get, set
        member val NextBaseExp = 0L with get, set
        member val NextJobExp = 0L with get, set
        member val StatusPoints = 0u with get, set
        member val SkillPoints = 0u with get, set
        
    type Equipment =
        {
            Id: uint16
            Index: int16
            Type: byte
            WearState: uint32
            IsIdentified: bool
            IsDamaged: bool
        }
        static member FromRaw (raw: RawEquipItem) =
            {
                Id = raw.Base.Id
                Index = raw.Base.Index
                Type = raw.Base.Type
                WearState = raw.Base.WearState
                IsIdentified = raw.Flags.IsIdentified
                IsDamaged = raw.Flags.IsDamaged
            }
        
    type Inventory =
        {
            WeightSoftCap: int
            Weight: uint32
            MaxWeight: uint32
            Zeny: int
            Equipment: Equipment list
        }
        
        static member Default =
            {WeightSoftCap=0;Weight=0u;MaxWeight=0u;Zeny=0;Equipment=list.Empty}
            
    module Inventory =
        let inline _Zeny f p = f p.Zeny <&> fun x -> {p with Zeny = x}
        let inline _Weight f p = f p.Weight <&> fun x -> {p with Weight = x}