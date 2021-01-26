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
  
type Status = Idle | Dead | Casting | Walking
        
type Unit =
    {
        Id: uint32
        Type: UnitType
        ActionId: Guid
        Status: Status
        TargetOfSkills: (SkillCast * Unit) list
        Casting: SkillCast option
        MaxHP: int
        HP: int
        SP: int32
        MaxSP: int32
        Name: string
        Position: (int * int)
        Speed: int16
    }
    
    static member Default = {
        Id = 0u
        Type = UnitType.Invalid
        ActionId = Guid.Empty
        Status = Idle
        TargetOfSkills = list.Empty
        Casting = None
        MaxHP = 0
        HP = 0
        MaxSP = 0
        SP = 0
        Name = ""
        Position = 0, 0
        Speed = 150s
    }
    
module Unit =
    let inline __position f p = f p.Position <&> fun x -> { p with Position = x }
    let inline _HP f p = f p.HP <&> fun x -> { p with HP = x }
    let inline _Status f p = f p.Status <&> fun x -> { p with Status = x }
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
        