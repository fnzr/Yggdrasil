namespace Yggdrasil.Game

open System
open NLog
open Yggdrasil.Types
open FSharpPlus.Lens

type UnitType =
  | Player
  | NPC
  | PC
  | Monster
  | Invalid  
  
type Action = Idle | Dead | Casting | Walking

type Unit =
    {
        Id: uint32
        Type: UnitType
        Action: Action
        TargetOfSkills: (SkillCast * Unit) list
        MaxHP: int
        HP: int
        SP: int32
        MaxSP: int32
        Name: string
        Position: (int16 * int16)
        Speed: int16
    }
    
    static member Default = {
        Id = 0u
        Type = UnitType.Invalid
        Action = Idle
        TargetOfSkills = list.Empty
        MaxHP = 0
        HP = 0
        MaxSP = 0
        SP = 0
        Name = ""
        Position = 0s, 0s
        Speed = 150s
    }
    
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
        