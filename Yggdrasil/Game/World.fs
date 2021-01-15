namespace Yggdrasil.Game

open System.Collections.Generic
open System.Diagnostics
open System.Threading
open NLog
open Yggdrasil
open Yggdrasil.Navigation
open Yggdrasil.Game.Event
open Yggdrasil.Types

type Connection(inbox: MailboxProcessor<GameEvent>) =
    static let stopwatch = Stopwatch()
    static do stopwatch.Start()
    let mutable status: ConnectionStatus = Inactive
    member this.Status
        with get() = status
        and set s =
            status <- s
            inbox.Post <| ConnectionStatus s
            
    member val TickOffset = 0L with get, set
    static member Tick with get() = stopwatch.ElapsedMilliseconds
    
type Goals() =
    let mutable position: (int * int) option = None
    member this.Logger = LogManager.GetLogger("Goals")
    member this.Position
        with get() = position
        and set v = position <- v
            
type Player(inbox: MailboxProcessor<GameEvent>) =
    
    let logger = LogManager.GetLogger "Player"
    let unit = {
        Name = ""
        Position = (0, 0)
        Speed = 150L
        Status = UnitStatus.Default()
        HP = 0
        MaxHP = 0
    }
    let mutable skills: Types.Skill list = []
    
    member val Inventory = Inventory ()
    member val BattleParameters = BattleParameters ()
    member val Level = Level ()
    member val Health = Health ()
    member val Goals = Goals()
    member this.Skills
        with get() = skills
        and set v = skills <- v
    member this.EventHandler event =
        unit.Status.Update event
        inbox.Post <| PlayerEvent event
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.Unit with get() = unit
    member val Dispatch: Types.Command -> unit = fun _ -> () with get, set
    member val Id = 0u with get, set
    member this.Status with get() = unit.Status
    member this.Position
        with get() = unit.Position
        and set v = unit.Position <- v
    member this.Name
        with get() = unit.Name
        and set v = unit.Name <- v
    member this.HP
        with get() = this.Unit.HP
        and set v = this.Unit.HP <- v
    member this.MaxHP
        with get() = this.Unit.MaxHP
        and set v = this.Unit.MaxHP <- v
        
    member this.Walk map destination delay =
        if this.WalkCancellationToken.IsSome then this.WalkCancellationToken.Value.Cancel()
        let walkFn = Movement.Walk (fun p -> this.Position <- p) this.EventHandler
        this.WalkCancellationToken <-
            Movement.StartMove map walkFn 
                this.Position
                destination
                delay (int this.Unit.Speed)
                
    member this.Disappear reason =
        match reason with
        | DisappearReason.Died -> this.EventHandler <| UnitEvent.Action Dead
        | r -> logger.Warn ("Unhandled disappear reason: {reason}", r)
     
type World(inbox: MailboxProcessor<GameEvent>) =    
    let mutable map: string = ""
    let units = Dictionary<uint32, NonPlayer>()
    let logger = LogManager.GetLogger("World")
    member this.Map
        with get() = map
        and set v =
            map <- v
            units.Clear()            
            inbox.Post <| MapChanged
    member this.MapData with get() = Maps.GetMapData map
    member this.Inbox with get() = inbox
    member this.GetUnit aid =
        let (success, unit) = units.TryGetValue aid
        if success then Some(unit)
        else None
        
    member this.SpawnUnit (npc: NonPlayer) =
        if units.TryAdd (npc.AID, npc) then
            inbox.Post <| match npc.Type with
                            | ObjectType.NPC -> UnitSpawn UnitSpawn.NPC
                            | ObjectType.Monster -> UnitSpawn UnitSpawn.Monster
                            | _ -> logger.Warn("Unhandled unit spawn")
                                   UnitSpawn UnitSpawn.Unknown
            logger.Info("Unit spawn: {type}:{name} ({aid})", npc.Type, npc.Unit.Name, npc.AID)
        else
            logger.Warn("Failed spawn unit {name} ({aid})", npc.FullName, npc.AID)
            
    member this.DespawnUnit aid =
        let (success, npc) = units.Remove aid        
        if success then
            inbox.Post <| UnitDespawn
            logger.Info("Unit despawn: {name} ({aid})", npc.FullName, aid)
        else logger.Warn("Failed despawning unit {aid}", aid)
        
    member this.PostEvent event delay =
        Async.Start <| async {
            do! Async.Sleep delay
            inbox.Post event
        }
        
type Game = {
    World: World
    Connection: Connection
    Player: Player
}