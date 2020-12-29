module Yggdrasil.Agent

open System.Collections.Generic
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Timers
open NLog
open Yggdrasil.Types
open Yggdrasil.Behavior

type Goals() =
    let Logger = LogManager.GetLogger("Goals")    
    let mutable position: (int * int) option = None
    member this.SetValue (field: byref<'T>, value) =
        if not <| EqualityComparer.Default.Equals(field, value) then
            Logger.Debug("{oldValue} => {newValue}", field, value)
            field <- value  
    member this.Position
        with get() = position
        and set v = this.SetValue (&position, v)
        
let AgentMachineLock = obj()

type Agent (name, map, dispatcher, initialMachineState, machineTransitions) as this =
    inherit EventDispatcher()

    let setupTimedBehavior (agent: Agent) =
        let timer = new Timer(200.0)
        timer.Elapsed.Add(agent.BehaviorTreeTick)
        timer.AutoReset <- true
        timer.Enabled <- true
    do setupTimedBehavior this
    let mutable destination: (int * int) option = None
    let mutable position = 0, 0
    let mutable map: string = map
    let mutable inventory = Inventory.Default
    let mutable battleParameters = BattleParameters.Default
    let mutable level = Level.Default
    let mutable skills: Skill list = []
    let mutable hpsp = HPSP.Default
    let mutable isConnected = false
    override this.Logger = LogManager.GetLogger("Agent")
        
    member val MachineState: StateMachine.ActiveMachineState<Agent> = initialMachineState with get, set
    
    member this.BehaviorTreeTick _ =
        let queue =  
            if this.MachineState.BehaviorQueue.IsEmpty then
                BehaviorTree.InitTree this.MachineState.State.Behavior
            else this.MachineState.BehaviorQueue
        let oldStatus = this.MachineState.Status
        let (queue, status) = BehaviorTree.Tick queue this
        this.MachineState <-
            { this.MachineState with
                BehaviorQueue = queue
                Status = status
            }
        if oldStatus <> this.MachineState.Status then
            this.DispatchEvent AgentEvent.BTStatusChanged
    member public this.DispatchEvent event =
        lock AgentMachineLock (fun _ ->
            this.MachineState <- this.MachineState.Transition machineTransitions event this
        )
    member this.Dispatcher: Command -> unit = dispatcher
    member this.Name: string = name
    member val Goals = Goals() with get, set
    member val TickOffset = 0L with get, set
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.Map
        with get() = map
        and set v = this.SetValue(&map, v, AgentEvent.MapChanged)
    member this.Destination
        with get() = destination
        and set v = this.SetValue(&destination, v, AgentEvent.DestinationChanged)
    member this.Position
        with get() = position
        and set v = this.SetValue(&position, v, AgentEvent.PositionChanged)
    member this.Inventory
        with get() = inventory
        and set v = this.SetValue(&inventory, v, AgentEvent.InventoryChanged)
    member this.BattleParameters
        with get() = battleParameters
        and set v = this.SetValue(&battleParameters, v, AgentEvent.BattleParametersChanged)
    member this.Level
        with get() = level
        and set v = this.SetValue(&level, v, AgentEvent.LevelChanged)
    member this.Skills
        with get() = skills
        and set v = this.SetValue(&skills, v, AgentEvent.SkillsChanged)
    member this.HPSP
        with get() = hpsp
        and set v = this.SetValue(&hpsp, v, AgentEvent.HPSPChanged)    
    member this.IsConnected
        with get() = isConnected
        and set v = this.SetValue(&isConnected, v, AgentEvent.ConnectionStatusChanged)

