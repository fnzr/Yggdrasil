namespace Yggdrasil.Game

open System.Diagnostics
open FSharpPlus.Lens
open Yggdrasil
open Yggdrasil.Game.Event
open Yggdrasil.Types

module Connection =
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let Tick () = stopwatch.ElapsedMilliseconds
    
type World =
    {
        IsConnected: bool
        IsMapReady: bool
        Player: Player
        Map: string
        ItemDrops: GroundItem list
        NPCs: Map<uint32, NonPlayer>
        TickOffset: int64
        Request: Request -> unit
        Inbox: (World -> World) -> unit
        Login: World -> unit
    }
    static member Default = {
        IsConnected = false
        IsMapReady = false
        Player = Player.Default
        Map = ""
        ItemDrops = list.Empty
        NPCs = Map.empty
        TickOffset = 0L
        Request = fun _ -> invalidOp "Request function not set"
        Inbox = fun _ -> invalidOp "Inbox not set"
        Login = fun _ -> invalidOp "Login function not set"
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
            
    let Ping world = Utils.Delay (fun () -> world.Inbox id)
