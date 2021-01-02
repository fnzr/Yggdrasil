module Yggdrasil.Agent

open System.Collections.Generic
open System.Threading
open System.Timers
open NLog
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Types
open Yggdrasil.Behavior

type Goals() =
    inherit EventDispatcher()
    let ev = Event<_>()
    let mutable position: (int * int) option = None
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger(e)
    override this.Logger = LogManager.GetLogger("Goals")
    member this.Position
        with get() = position
        and set v = this.SetValue (&position, v, AgentEvent.GoalPositionChanged)
        
type Agent (name, map, dispatcher) =
    inherit EventDispatcher()
    let mutable skills: Skill list = []
    let mutable isConnected = false
    let mutable btStatus = BehaviorTree.Invalid
    
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
        
    member this.DelayPing millis =
        let timer = new Timer(millis)
        timer.Elapsed.Add(fun _ -> this.Dispatch AgentEvent.Ping)
        timer.AutoReset <- false
        timer.Enabled <- true

let EventMailbox (agent: Agent) stateMachine initialState (inbox: MailboxProcessor<AgentEvent>) =
    let rec loop (currentMachine: ActiveMachineState<Agent>) currentTree = async {
        let! event = inbox.Receive()
        let tree, machine =
            match currentMachine.Transition stateMachine event agent with
            | Some (newState) ->
                //cancel BT
                agent.BTStatus <- BehaviorTree.Invalid
                BehaviorTree.InitTree newState.State.Behavior,                
                newState
            | None -> currentTree, currentMachine
            
        let nextTree =
            if tree.IsEmpty || tree.Events.Contains event then
                let (queue, status) = BehaviorTree.Tick tree agent
                agent.BTStatus <- status
                queue
            else tree
        return! loop machine nextTree
    }
    let machine = ActiveMachineState<Agent>.Create initialState
    let bt = BehaviorTree.InitTree initialState.Behavior
    loop machine bt
    
let SetupAgentBehavior stateMachine initialState (agent: Agent) =
    let mailbox = MailboxProcessor.Start(EventMailbox agent stateMachine initialState)
            
    agent.OnEventDispatched.Add(mailbox.Post)
    agent.Location.OnEventDispatched.Add(mailbox.Post)
    agent.Inventory.OnEventDispatched.Add(mailbox.Post)
    agent.BattleParameters.OnEventDispatched.Add(mailbox.Post)
    agent.Level.OnEventDispatched.Add(mailbox.Post)
    agent.Health.OnEventDispatched.Add(mailbox.Post)
    agent.Goals.OnEventDispatched.Add(mailbox.Post)