module Yggdrasil.Behavior.StateMachine

open System
open FSharpx.Collections
open NLog

let Logger = LogManager.GetLogger("StateMachine")

type MachineState<'T> =
    {
        Tag: string
        Condition: 'T -> bool
        OnEnter: 'T -> unit
        OnLeave: 'T -> unit
        Behavior: BehaviorTree.Factory<'T>
    }
    
    override this.Equals o =
            match o with
            | :? MachineState<'T> as y -> this.Tag.Equals(y.Tag)
            | _ -> false
        override this.GetHashCode () = this.Tag.GetHashCode()
        interface IComparable with
            override this.CompareTo o =
                match o with
                | :? MachineState<'T> as y -> this.Tag.CompareTo(y.Tag)
                | _ -> invalidArg (string o) "Invalid comparison for State"
    
type ActiveMachineState<'T> =
    {
        State: MachineState<'T>
        BehaviorQueue: Queue<BehaviorTree.Node<'T>>
        Status: BehaviorTree.Status
    }
    static member Create state = {
        State = state
        BehaviorQueue = Queue.empty
        Status = BehaviorTree.Running
    }
type TransitionsMap<'T> = Map<MachineState<'T>,(MachineState<'T> * ('T -> BehaviorTree.Status -> bool)) []>
let FindTransition data status (state: MachineState<'T>, condition: 'T -> BehaviorTree.Status -> bool) =
    state.Condition data && condition data status
    
let Tick (transitionsMap: TransitionsMap<'T>) (activeState: ActiveMachineState<'T>) data =
    let transitions = match transitionsMap.TryFind activeState.State with
                        | Some (t) -> t
                        | None -> [||]
    let next = match Array.tryFind (FindTransition data activeState.Status) transitions with
                | Some(state, _) ->
                    Logger.Info ("{oldState} => {newState}", activeState.State.Tag, state.Tag)
                    activeState.State.OnLeave data
                    state.OnEnter data
                    ActiveMachineState<'T>.Create state                    
                | None -> activeState
    let q = if next.BehaviorQueue.IsEmpty then
                BehaviorTree.InitTree next.State.Behavior
            else next.BehaviorQueue
    let (queue, status) = BehaviorTree.Tick q data
    { next with
        BehaviorQueue = queue
        Status = status
    }
