namespace Yggdrasil.Game

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open NLog
open Yggdrasil
open Yggdrasil.Navigation
open Yggdrasil.Game.Event

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
        
type UnitStatus =
    {
        mutable Action: Action
    }
    static member Default () = {
        Action = Idle
    }
            
type Player(inbox: MailboxProcessor<GameEvent>) =
    let unit = {
        Name = ""
        Position = (0, 0)
        Speed = 150L
    }
    let mutable skills: Types.Skill list = []
    
    member val Inventory = Inventory ()
    member val BattleParameters = BattleParameters ()
    member val Level = Level ()
    member val Health = Health ()
    member val Goals = Goals()
    member val Status = UnitStatus.Default()
    member this.Skills
        with get() = skills
        and set v = skills <- v
    member this.EventHandler event =
        match event with
        | Action a -> this.Status.Action <- a
        inbox.Post <| PlayerEvent event
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.Unit with get() = unit
    member val Dispatch: Types.Command -> unit = fun _ -> () with get, set
    member this.Position
        with get() = unit.Position
        and set v = unit.Position <- v
    member this.Name
        with get() = unit.Name
        and set v = unit.Name <- v
     
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