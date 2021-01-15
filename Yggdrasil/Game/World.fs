namespace Yggdrasil.Game

open System.Collections.Generic
open System.Diagnostics
open NLog
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
    static let PlayerEventBuilder event = (PlayerEvent event) :> GameEvent
    let logger = LogManager.GetLogger "Player"
    let unit = Unit(PlayerEventBuilder, inbox, 0u, "", (0,0), 0s, 0, 0, logger)
    let mutable skills: Skill list = []
    
    member val Inventory = Inventory ()
    member val BattleParameters = BattleParameters ()
    member val Level = Level ()
    member val Health = Health ()
    member val Goals = Goals()
    member this.Skills
        with get() = skills
        and set v = skills <- v
    member this.Unit with get() = unit
    member this.Id
        with get() = unit.Id
        and set v = unit.Id <- v
    member this.Position
        with get() = unit.Position
        and set v = unit.Position <- v
    member this.Name
        with get() = unit.Name
        and set v = unit.Name <- v
    member this.Status with get() = unit.Status
    member val Dispatch: Command -> unit = fun _ -> () with get, set
     
type World(inbox: MailboxProcessor<GameEvent>) =    
    let player = Player(inbox)
    let mutable map: string = ""
    let npcs = Dictionary<uint32, NonPlayer>()
    let logger = LogManager.GetLogger("World")
    member this.Map
        with get() = map
        and set v =
            map <- v
            npcs.Clear()            
            inbox.Post <| MapChanged
    member this.MapData with get() = Maps.GetMapData map
    member this.Player with get() = player
    member this.Inbox with get() = inbox
    member this.GetUnit aid =
        if player.Id = aid then Some player.Unit
        else
            let (success, npc) = npcs.TryGetValue aid
            if success then Some npc.Unit
            else None
        
    member this.SpawnUnit (npc: NonPlayer) =
        if npcs.TryAdd (npc.Id, npc) then
            inbox.Post <| match npc.Type with
                            | ObjectType.NPC -> UnitSpawn UnitSpawn.NPC
                            | ObjectType.Monster -> UnitSpawn UnitSpawn.Monster
                            | ObjectType.PlayerCharacter -> UnitSpawn UnitSpawn.Player
                            | _ -> logger.Warn("Unhandled unit spawn")
                                   UnitSpawn UnitSpawn.Unknown
            logger.Info("Unit spawn: {type}:{name} ({aid})", npc.Type, npc.Unit.Name, npc.Id)
        else
            logger.Warn("Failed spawn unit {name} ({aid})", npc.FullName, npc.Id)
            
    member this.DespawnUnit aid reason =
        if player.Id = aid then player.Unit.Disappear reason
        else
            let (success, npc) = npcs.Remove aid        
            if success then npc.Unit.Disappear reason
            else logger.Warn("Failed despawning unit {aid}", aid)
        
    member this.PostEvent event delay =
        Async.Start <| async {
            do! Async.Sleep delay
            inbox.Post event
        }
        
type Game = {
    World: World
    Connection: Connection
}