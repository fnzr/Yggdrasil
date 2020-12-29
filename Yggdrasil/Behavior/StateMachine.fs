module Yggdrasil.Behavior.StateMachine

open System
open FSharpx.Collections
open NLog
open Yggdrasil.Types

let Logger = LogManager.GetLogger("StateMachine")

let checkTransition data e (_, event, condition) = e = event && condition data

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

type Transition<'T> = MachineState<'T> * AgentEvent * ('T -> bool)
type TMap<'T> = Map<MachineState<'T>,Transition<'T> []>
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
    
    member this.Transition (transitionsMap: TMap<'T>) event data =
        let transitions = match transitionsMap.TryFind this.State with
                            | Some (t) -> t
                            | None -> [||]
        match Array.tryFind (checkTransition data event) transitions with
        | Some(state, _, _) ->
            Logger.Info ("{oldState} => {newState}", this.State.Tag, state.Tag)
            this.State.OnLeave data
            state.OnEnter data
            ActiveMachineState<'T>.Create state
        | None -> this
        
type TransitionsMap<'T> = Map<MachineState<'T>,(MachineState<'T> * ('T -> BehaviorTree.Status -> bool)) []>
