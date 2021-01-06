module Yggdrasil.Agent

open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Timers
open NLog
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Types
open Yggdrasil.Behavior

let Stopwatch = Stopwatch()
Stopwatch.Start()
let GetCurrentTick() = Stopwatch.ElapsedMilliseconds

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
        Navigation.Pathfinding.ManhattanDistance this.Position point
        
    member this.PathTo point =
        let mapData = Navigation.Maps.GetMapData this.Map
        Navigation.Pathfinding.AStar mapData this.Position point
        
type Agent () =
    inherit EventDispatcher()
    let mutable skills: Skill list = []
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger e
    override this.Logger = LogManager.GetLogger("Agent")
        
    member val Dispatcher: Command -> unit = fun _ -> () with get, set
    member val Name: string = "" with get, set   
    member val Location = Location ()
    member val Inventory = Inventory ()
    member val BattleParameters = BattleParameters ()
    member val Level = Level ()
    member val Health = Health ()    
    member val Goals = Goals ()
    
    member val TickOffset = 0L with get, set
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.Skills
        with get() = skills
        and set v = skills <- v//this.SetValue(&skills, v, AgentEvent.SkillsChanged)
    member this.DelayPing millis =
        let timer = new Timer(millis)
        timer.Elapsed.Add(fun _ -> this.Dispatch AgentEvent.Ping)
        timer.AutoReset <- false
        timer.Enabled <- true
        
let rec AdvanceBehavior agent (stateMachine: StateMachine<State, AgentEvent, Agent>, tree) =
    let (queue, status) = BehaviorTree.Tick tree agent
    let next = match status with
                | BehaviorTree.Success -> stateMachine.TryTransit BehaviorTreeSuccess agent
                | BehaviorTree.Failure -> stateMachine.TryTransit BehaviorTreeFailure agent
                | _ -> None
    match next with
    | Some machine ->
        (machine, BehaviorTree.InitTreeOrEmpty machine.CurrentState.Behavior)
            |> AdvanceBehavior agent
    | None -> stateMachine, queue

let EventMailbox (agent: Agent) stateMachine (inbox: MailboxProcessor<AgentEvent>) =
    let rec loop (currentMachine: StateMachine<State, AgentEvent, Agent>) currentTree = async {
        let! event = inbox.Receive()
        
        if event = AgentEvent.MapChanged then agent.Dispatcher DoneLoadingMap
        
        let machine, queue = 
            match currentMachine.TryTransit event agent with
                | Some m -> m, BehaviorTree.InitTreeOrEmpty m.CurrentState.Behavior
                | None -> currentMachine, currentTree
            |> AdvanceBehavior agent
        return! loop machine queue
    }
    let bt = FSharpx.Collections.Queue.empty
    loop stateMachine bt
    
let StartAgent server username password machineFactory =
    let machine = machineFactory server username password
    let agent = Agent ()
    
    let mailbox = MailboxProcessor.Start(EventMailbox agent machine)
    mailbox.Error.Add <| Logger.Error

    agent.OnEventDispatched.Add(mailbox.Post)
    agent.Location.OnEventDispatched.Add(mailbox.Post)
    agent.Inventory.OnEventDispatched.Add(mailbox.Post)
    agent.BattleParameters.OnEventDispatched.Add(mailbox.Post)
    agent.Level.OnEventDispatched.Add(mailbox.Post)
    agent.Health.OnEventDispatched.Add(mailbox.Post)
    agent.Goals.OnEventDispatched.Add(mailbox.Post)
    
    machine.Start agent