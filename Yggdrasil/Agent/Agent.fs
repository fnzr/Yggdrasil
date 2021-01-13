namespace Yggdrasil.Agent

open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Timers
open NLog
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Types
open Yggdrasil.Behavior

type Goals() =
    let logger = LogManager.GetLogger("Goals")
    let mutable position: (int * int) option = None
    member this.Position
        with get() = position
        and set v = Yggdrasil.Utils.SetValue logger &position v "GoalPositionChanged" |> ignore
        
type AgentBehaviorTree(root: BehaviorTree.Factory<Agent>, agent: Agent) as this =
    let timer = new Timer(150.0)
    do timer.Enabled <- true
    do timer.AutoReset <- true
    do timer.Elapsed.Add(this.Tick)
    let mutable queue = FSharpx.Collections.Queue.empty
    let data = Dictionary<string, obj>()
    member this.Data = data
    
    member this.Restart () = timer.Enabled <- true
    member this.Tick _ =
        if queue.IsEmpty then
            queue <- BehaviorTree.InitTree root
        let (q, status) = BehaviorTree.Tick queue agent
        match status with
            | BehaviorTree.Status.Success ->
                agent.Publish <| BehaviorResult Success
                timer.Enabled <- false
            | BehaviorTree.Status.Failure ->
                agent.Publish <| BehaviorResult Failure
                timer.Enabled <- false
            | _ -> ()
        queue <- q 

and Agent () =
    static let stopwatch = Stopwatch()
    static do stopwatch.Start()
    let mutable skills: Skill list = []
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    member this.Logger = LogManager.GetLogger("Agent")
        
    member val Dispatcher: Command -> unit = fun _ -> () with get, set
    member val Name: string = "" with get, set   
    member val Location = Location (ev.Trigger)
    member val Inventory = Inventory ()
    member val BattleParameters = BattleParameters ()
    member val Level = Level ()
    member val Health = Health ()    
    member val Goals = Goals ()
    member val BehaviorTree: AgentBehaviorTree option = None with get, set
    
    member val TickOffset = 0L with get, set
    member val WalkCancellationToken: CancellationTokenSource option = None with get, set
    member this.Publish event = ev.Trigger event
    member this.Skills
        with get() = skills
        and set v = skills <- v
    static member Tick with get() = stopwatch.ElapsedMilliseconds
    
    member this.ChangeMap map =
        this.Location.Map <- map
        this.Dispatcher DoneLoadingMap
