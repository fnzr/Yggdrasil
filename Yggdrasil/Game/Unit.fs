namespace Yggdrasil.Game

open System
open NLog
open Yggdrasil.Types
open FSharpPlus.Lens

    
module Unit =
    let inline __position f p = f p.Position <&> fun x -> { p with Position = x }
    let inline _HP f p = f p.HP <&> fun x -> { p with HP = x }
    let inline _Status f p = f p.Action <&> fun x -> { p with Action = x }
    let inline _MaxHP f p = f p.MaxHP <&> fun x -> { p with MaxHP = x }
    
[<AutoOpen>]
module UnitFactory =
    let Logger = LogManager.GetLogger("Unit")
    let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) position =        
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> UnitType.NPC
                    | 0x0uy -> UnitType.PC
                    | 0x5uy -> UnitType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            UnitType.Invalid
        {Unit.Default with
            Id = raw1.AID
            Type = oType
            MaxHP = raw2.MaxHP
            HP = raw2.HP
            Name = raw2.Name.Split("#").[0]
            Position = position
            Speed = raw1.Speed}
        