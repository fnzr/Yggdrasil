namespace Yggdrasil.Game

open System.Threading
open NLog
open Yggdrasil.Types
open Yggdrasil.Game.Event
open Yggdrasil.Game.Skill
open Yggdrasil.Utils

type ObjectType =
  | NPC
  | Monster
  | Invalid
  | PlayerCharacter
  
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
        | _ -> ()
        
  
type Unit(eventBuilder: UnitEvent -> GameEvent, inbox: MailboxProcessor<GameEvent>,
          id, name, position, speed, hp, maxHP, logger: Logger) =
    let mutable walkCancellationToken: CancellationTokenSource option = None
    let mutable castCancellationToken: CancellationTokenSource option = None
    let mutable targetedBySkills: (SkillCast * Unit) list = []
    let mutable castInfo: SkillCast option = None
    let status = UnitStatus.Default()
    member val Id: uint32 = id with get, set
    member val Name: string = name with get, set
    member val Position: int * int = position with get, set
    member val Speed: int16 = speed with get, set
    member val HP: int = hp with get, set
    member val MaxHP: int = maxHP with get, set
    member this.Status with get() = status
    member this.CastInfo with get() = castInfo    
    member this.Dispatch event =
        this.Status.Update event
        inbox.Post <| eventBuilder event
    
    member this.Walk map destination delay =
        if walkCancellationToken.IsSome then walkCancellationToken.Value.Cancel()
        let walkFn = Movement.Walk (fun p -> this.Position <- p) this.Dispatch
        walkCancellationToken <-
            Movement.StartMove map walkFn this.Position
                destination delay (int this.Speed)
                
    member this.StartCast (cast: SkillCast) (target: Unit) =
        if castCancellationToken.IsSome then castCancellationToken.Value.Cancel()
        let tokenSource = new CancellationTokenSource()
        castCancellationToken <- Some <| tokenSource
        let executeCast () =
            if tokenSource.Token.IsCancellationRequested then
                logger.Debug("{name} cancelled cast {skill} on {target}", this.Name, cast.SkillId, target.Name)
            else
                logger.Debug("{name} cast {skill} on {target}", this.Name, cast.SkillId, target.Name)
            target.OnTargetedBySkillEnd cast this tokenSource.Token.IsCancellationRequested
            castInfo <- None
            this.Dispatch <| Action Idle
        this.Dispatch <| Action Casting
        target.OnTargetedBySkillStart cast this
        castInfo <- Some cast
        Delay executeCast cast.Delay
        
    member this.OnTargetedBySkillStart (cast: SkillCast) (source: Unit) =
        targetedBySkills <- (cast, source) :: targetedBySkills
        this.Dispatch <| TargetedBySkill
        
    member this.OnTargetedBySkillEnd (cast: SkillCast) (source: Unit) success =
        targetedBySkills <- List.except [cast, source] targetedBySkills
                
    member this.Disappear reason =
        match reason with
        | DisappearReason.Died -> this.Dispatch <| Action Dead
        | r -> logger.Warn ("Unhandled disappear reason: {reason}", r)
        

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
    
    let UnitEventBuilder event = (UnitEvent event) :> GameEvent 
    let NPCLogger = LogManager.GetLogger "NPC"
    let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) (inbox: MailboxProcessor<GameEvent>) =        
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> ObjectType.NPC
                    | 0x0uy -> ObjectType.PlayerCharacter
                    | 0x5uy -> ObjectType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            ObjectType.Invalid
                            //id, name, position, speed, hp, maxHP,
        {
            FullName = raw2.Name
            Type = oType            
            Unit = Unit(UnitEventBuilder, inbox, raw1.AID,
                        raw2.Name.Split('#').[0], (int raw2.PosX, int raw2.PosY),
                         raw1.Speed, raw2.HP, raw2.MaxHP, NPCLogger)
        }
        