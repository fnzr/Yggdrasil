namespace Yggdrasil.Game

open System.Diagnostics
open FSharpPlus.Lens
open Yggdrasil.Types

module Connection =
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let Tick () = stopwatch.ElapsedMilliseconds

type World =
    {
        Player: Player
        Map: string
        ItemsOnGround: GroundItem list
        NPCs: Map<uint32, NonPlayer>
        TickOffset: int64
    }
    static member Default = {
        Player = Player.Default
        Map = ""
        ItemsOnGround = list.Empty
        NPCs = Map.empty
        TickOffset = 0L
    }

module World =
    let inline _Player f p = f p.Player <&> fun x -> { p with Player = x }
    let Unit (world: World) id =        
        if world.Player.Id = id then Some world.Player.Unit
        else
            match world.NPCs.TryFind id with
            | Some npc -> Some npc.Unit
            | None -> None
            
    let withPlayerPosition position world =
        setl _Player <|
            setl Player._Position position world.Player
        <| world
        
    let UpdateUnit (unit: Unit) world =
        if unit.Id = world.Player.Id then
            let p = setl Player._Unit unit world.Player
            {world with Player = p}
        else
            let npc = world.NPCs.[unit.Id]
            {world with NPCs = world.NPCs.Add(npc.Id, {npc with Unit = unit})}
            
(*
type World(inbox: MailboxProcessor<Event.GameEvent>) =    
    let player = Player(inbox)
    let mutable map: string = ""
    let mutable droppedItems: GroundItem list = []
    let npcs = Dictionary<uint32, NonPlayer>()
    let logger = LogManager.GetLogger("World")
    member this.Map
        with get() = map
        and set v =
            map <- v
            npcs.Clear()            
            inbox.Post <| Event.MapChanged
    member this.MapData with get() = Maps.GetMapData map
    member this.Player with get() = player
    member this.Inbox with get() = inbox
    
    member val TickOffset = 0L with get, set
    member this.GetUnit aid =
        if player.Id = aid then Some player.Unit
        else
            let (success, npc) = npcs.TryGetValue aid
            if success then Some npc.Unit
            else None
        
    member this.SpawnUnit (npc: NonPlayer) =
        if npcs.TryAdd (npc.Id, npc) then
            inbox.Post <| match npc.Type with
                            | ObjectType.NPC -> Event.UnitSpawn Event.NPC
                            | ObjectType.Monster -> Event.UnitSpawn Event.Monster
                            | ObjectType.PlayerCharacter -> Event.UnitSpawn Event.Player
                            | _ -> logger.Warn("Unhandled unit spawn")
                                   Event.UnitSpawn Event.Unknown
            logger.Info("Unit spawn: {type}:{name} ({aid})", npc.Type, npc.Unit.Name, npc.Id)
        else
            logger.Warn("Failed spawn unit {name} ({aid})", npc.FullName, npc.Id)
            
    member this.DespawnUnit aid reason =
        if player.Id = aid then player.Unit.Disappear reason
        else
            let (success, npc) = npcs.Remove aid        
            if success then npc.Unit.Disappear reason
            else logger.Warn("Failed despawning unit {aid}", aid)
    member this.ItemDrop (item: GroundItem) =
        droppedItems <- item :: droppedItems
        inbox.Post <| Event.ItemDropped
        
    member this.ItemDropDisappear id =
        droppedItems <- List.where (fun i -> i.Id <> id) droppedItems
        inbox.Post <| Event.ItemDroppedDisappeared
        
    member this.PostEvent event delay =
        Async.Start <| async {
            do! Async.Sleep delay
            inbox.Post event
        }
*)