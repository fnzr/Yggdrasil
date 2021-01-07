namespace Yggdrasil.Agent

open NLog
open Yggdrasil.Types
open Yggdrasil.Navigation

type Location () =
    inherit EventDispatcher()
    let ev = Event<_>()
    let mutable map: string = ""
    let mutable position = 0, 0
    let mutable destination: (int * int) option = None
    
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger(e)
    override this.Logger = LogManager.GetLogger("Location")
    member this.Map
        with get() = map
        and set v = this.SetValue(&map, v, AgentEvent.MapChanged)
    member this.Destination
        with get() = destination
        and set v = this.SetValue(&destination, v, AgentEvent.DestinationChanged)
    member this.Position
        with get() = position
        and set v = this.SetValue(&position, v, AgentEvent.PositionChanged)
        
    member this.DistanceTo point =
        Pathfinding.ManhattanDistance this.Position point
        
    member this.PathTo point leeway =
        let mapData = Maps.GetMapData this.Map
        Pathfinding.AStar mapData this.Position point leeway