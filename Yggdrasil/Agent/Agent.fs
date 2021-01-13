module Yggdrasil.Agent.Agent

open System.Collections.Generic
open System.Diagnostics
open System.Threading
open NLog
open Yggdrasil.Agent.Event
open Yggdrasil.Agent.Unit
open Yggdrasil.Types

type Goals() =
    let logger = LogManager.GetLogger("Goals")
    let mutable position: (int * int) option = None
    member this.Position
        with get() = position
        and set v = Yggdrasil.Utils.SetValue logger &position v "GoalPositionChanged" |> ignore

type Agent () =
    static let stopwatch = Stopwatch()
    static do stopwatch.Start()
    let mutable skills: Skill list = []
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    member this.Logger = LogManager.GetLogger("Agent")
        
    member val Dispatch: Command -> unit = fun _ -> () with get, set
    member val Name: string = "" with get, set   
    member val Location = Location.Location (ev.Trigger)
    member val Inventory = Components.Inventory ()
    member val BattleParameters = Components.BattleParameters ()
    member val Level = Components.Level ()
    member val Health = Components.Health ()    
    member val Goals = Goals ()
    
    member val TickOffset = 0L with get, set
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member val private Units = Dictionary<uint32, Unit.Unit>()
    member this.Publish event = ev.Trigger event
    member this.Skills
        with get() = skills
        and set v = skills <- v
    static member Tick with get() = stopwatch.ElapsedMilliseconds
    
    member this.ChangeMap map =
        this.Location.Map <- map
        this.Units.Clear ()
        this.Dispatch DoneLoadingMap
        
    member this.SpawnUnit (unit: Unit.Unit) =
        if this.Units.TryAdd (unit.AID, unit) then
            let event = match unit.Type with
                        | ObjectType.NPC -> UnitSpawn UnitSpawn.NPC
                        | Monster -> UnitSpawn UnitSpawn.Monster
                        | _ -> Logger.Warn("Unhandled unit spawn")
                               UnitSpawn UnitSpawn.Unknown
            this.Publish event
            this.Logger.Info("Unit spawn: {type}:{name} ({aid})", unit.Type, unit.Name, unit.AID)
        else
            this.Logger.Warn("Failed spawn unit {name} ({aid})", unit.FullName, unit.AID)
            
    member this.DespawnUnit aid =
        if this.Units.Remove aid then
            this.Publish UnitDespawn
            this.Logger.Info("Unit despawn: {aid}", aid)
        else Logger.Warn("Failed despawning unit {aid}", aid)
        
    member this.Unit aid =
        let (success, unit) = this.Units.TryGetValue aid
        if success then Some(unit)
        else None
