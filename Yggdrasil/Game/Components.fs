namespace Yggdrasil.Game

open FSharpPlus.Lens
open Yggdrasil.Types

module Components = 
    type Level =
        {
            BaseLevel: uint32
            JobLevel: uint32
            BaseExp: int64
            JobExp: int64
            NextBaseExp: int64
            NextJobExp: int64
        }
        static member Default =
         {BaseLevel=0u;JobLevel=0u;BaseExp=0L;JobExp=0L;
         NextBaseExp=0L;NextJobExp=0L}
        
    type Equipment =
        {
            Id: uint16
            Index: int16
            Type: byte
            Location: uint32
            WearState: uint32
            IsIdentified: bool
            IsDamaged: bool
        }
        static member FromRaw (raw: RawEquipItem) =
            {
                Id = raw.Base.Id
                Index = raw.Base.Index
                Type = raw.Base.Type
                Location = raw.Base.Location
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
        }
        
        static member Default =
            {WeightSoftCap=0;Weight=0u;MaxWeight=0u;Zeny=0}
            
    module Inventory =
        let inline _Zeny f p = f p.Zeny <&> fun x -> {p with Zeny = x}
        let inline _Weight f p = f p.Weight <&> fun x -> {p with Weight = x}