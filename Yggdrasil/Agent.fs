module Yggdrasil.Agent

open System.Collections.Generic
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Timers
open NLog
open Yggdrasil.Behavior.StateMachine
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


type Agent (name, map, dispatcher) =
    inherit EventDispatcher()
    let mutable skills: Skill list = []
    let mutable isConnected = false
    let mutable btStatus = BehaviorTree.Running
    
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger e
    override this.Logger = LogManager.GetLogger("Agent")
        
    member val Dispatcher: Command -> unit = dispatcher
    member val Name: string = name    
    member val Location = Location (map)
    member val Inventory = Inventory ()
    member val BattleParameters = BattleParameters ()
    member val Level = Level ()
    member val Health = Health ()
    
    member val Goals = Goals() with get, set
    member val TickOffset = 0L with get, set
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.Skills
        with get() = skills
        and set v = this.SetValue(&skills, v, AgentEvent.SkillsChanged)
    
    member this.IsConnected
        with get() = isConnected
        and set v = this.SetValue(&isConnected, v, AgentEvent.ConnectionStatusChanged)
        
    member this.BTStatus
        with get() = btStatus
        and set v = this.SetValue(&btStatus, v, AgentEvent.BTStatusChanged)
        
    member this.ScheduleBTTick millis =
        use timer = new Timer(millis)
        timer.Elapsed.Add(fun _ -> this.Dispatch AgentEvent.RequestBTTick)
        timer.AutoReset <- false
        timer.Enabled <- true
    
    
let SetupAgentBehavior stateMachine initialState (agent: Agent) =
    let mutable state = ActiveMachineState<Agent>.Create initialState

    let behaviorTick _ =
        let queue =  
            if state.BehaviorQueue.IsEmpty then
                BehaviorTree.InitTree state.State.Behavior
            else state.BehaviorQueue
        let (queue, status) = BehaviorTree.Tick queue agent
        state <-
            { state with
                BehaviorQueue = queue
                Status = status
            }
        agent.BTStatus <- state.Status
            
    let dispatchEvent event =
        if event = AgentEvent.RequestBTTick then
            behaviorTick ()
        else
            lock AgentMachineLock (fun _ ->
            state <- state.Transition stateMachine event agent
            )
            
    agent.OnEventDispatched.Add(dispatchEvent)
    agent.Location.OnEventDispatched.Add(dispatchEvent)
    agent.Inventory.OnEventDispatched.Add(dispatchEvent)
    agent.BattleParameters.OnEventDispatched.Add(dispatchEvent)
    agent.Level.OnEventDispatched.Add(dispatchEvent)
    agent.Health.OnEventDispatched.Add(dispatchEvent)
