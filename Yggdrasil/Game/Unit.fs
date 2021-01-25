namespace Yggdrasil.Game

open System
open NLog
open Yggdrasil.Types
open Yggdrasil.Game.Skill
open FSharpPlus.Lens

type ObjectType =
  | NPC
  | Monster
  | Invalid
  | PlayerCharacter
  
type Status = Idle | Dead | Casting | Walking
        
type Unit =
    {
        Id: uint32
        ActionId: Guid
        Status: Status
        TargetOfSkills: (SkillCast * Unit) list
        Casting: SkillCast option
        MaxHP: int
        HP: int        
        Name: string
        Position: (int * int)
        Speed: int16
    }
    
    static member Default = {
        Id = 0u
        ActionId = Guid.Empty
        Status = Idle
        TargetOfSkills = list.Empty
        Casting = None
        MaxHP = 0
        HP = 0        
        Name = ""
        Position = 0, 0
        Speed = 150s
    }
    
module Unit =
    let inline __position f p = f p.Position <&> fun x -> { p with Position = x }
    let inline _HP f p = f p.HP <&> fun x -> { p with HP = x }
    let inline _Status f p = f p.Status <&> fun x -> { p with Status = x }
    let inline _MaxHP f p = f p.MaxHP <&> fun x -> { p with MaxHP = x }

type NonPlayer =
    {
        Type: ObjectType        
        FullName: string
        Unit: Unit
    }
    member this.Id = this.Unit.Id
    
[<AutoOpen>]
module UnitFactory =
    let Logger = LogManager.GetLogger("Unit")
    
    let NPCLogger = LogManager.GetLogger "NPC"
    let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) position =        
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> ObjectType.NPC
                    | 0x0uy -> ObjectType.PlayerCharacter
                    | 0x5uy -> ObjectType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            ObjectType.Invalid
        {
            FullName = raw2.Name
            Type = oType            
            Unit = {Unit.Default with
                     Id = raw1.AID
                     MaxHP = raw2.MaxHP
                     HP = raw2.HP
                     Name = raw2.Name.Split("#").[0]
                     Position = position
                     Speed = raw1.Speed}
                     
        }
        