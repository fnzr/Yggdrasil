namespace Yggdrasil.Game

open System
open NLog
open Yggdrasil.Types
open Yggdrasil.Game.Event

type ObjectType =
  | NPC
  | Monster
  | Invalid
  
type UnitStatus =
    {
        mutable Action: Action
    }
    static member Default () = {
        Action = Idle
    }
    member this.Update event =
        match event with
        | Action a -> this.Action <- a
        
  
type Unit =
    {
        mutable Name: string
        mutable Position: int * int
        mutable Speed: int64
        mutable HP: int
        mutable MaxHP: int
        Status: UnitStatus
    }

type NonPlayer =
    {
        Type: ObjectType
        AID: uint32
        Unit: Unit
        FullName: string
        Inbox: MailboxProcessor<GameEvent>
    }
    member this.HP
        with get() = this.Unit.HP
        and set v = this.Unit.HP <- v
    member this.MaxHP
        with get() = this.Unit.MaxHP
        and set v = this.Unit.MaxHP <- v
        
    member this.EventHandler event =
        this.Unit.Status.Update event
        this.Inbox.Post <| UnitEvent event
    
[<AutoOpen>]
module UnitFactory =
    let Logger = LogManager.GetLogger("Unit")
    let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) (inbox: MailboxProcessor<GameEvent>) =        
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
                Status = UnitStatus.Default()
                HP = raw2.HP
                MaxHP = raw2.MaxHP
            }
            AID = raw1.AID
            FullName = raw2.Name
            Type = oType
            Inbox = inbox
        }