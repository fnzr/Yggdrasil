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
        
type Agent () =
    inherit EventDispatcher()
    static let stopwatch = Stopwatch()
    static do stopwatch.Start()
    let mutable skills: Skill list = []
    let behaviorData = Dictionary<string, obj>()
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
    member this.BehaviorData = behaviorData
    member this.Skills
        with get() = skills
        and set v = skills <- v//this.SetValue(&skills, v, AgentEvent.SkillsChanged)
    member this.DelayPing millis =
        let timer = new Timer(millis)
        timer.Elapsed.Add(fun _ -> this.Dispatch AgentEvent.Ping)
        timer.AutoReset <- false
        timer.Enabled <- true
    static member Tick with get() = stopwatch.ElapsedMilliseconds
