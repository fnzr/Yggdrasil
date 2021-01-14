namespace Yggdrasil.Game

open System
open NLog
open Yggdrasil.Types

type ObjectType =
  | NPC
  | Monster
  | Invalid
  
type Unit =
    {
        mutable Name: string
        mutable Position: int * int
        mutable Speed: int64
    }

type NonPlayer =
    {
        Type: ObjectType
        AID: uint32
        Unit: Unit
        FullName: string
    }
    
[<AutoOpen>]
module UnitFactory =
    let Logger = LogManager.GetLogger("Unit")
    let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) =        
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> ObjectType.NPC
                    | 0x5uy -> ObjectType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            ObjectType.Invalid
        {
            Unit = {
                Name = raw2.Name.Split('#').[0]
                Position = (int raw2.PosX, int raw2.PosY)
                Speed = Convert.ToInt64 raw1.Speed
            }
            AID = raw1.AID
            FullName = raw2.Name
            Type = oType
        }