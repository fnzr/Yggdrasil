module Yggdrasil.Behavior.StateMachine

open System
open FSharpx.Collections

type State<'T> =
    {
        Tag: string
        Condition: 'T -> bool
        OnEnter: 'T -> unit
        BehaviorRoot: BehaviorTree.Factory<'T>
    }
    override this.Equals o =
        match o with
        | :? State<'T> as y -> this.Tag.Equals(y.Tag)
        | _ -> false
    override this.GetHashCode () = this.Tag.GetHashCode()
    interface IComparable with
        override this.CompareTo o =
            match o with
            | :? State<'T> as y -> this.Tag.CompareTo(y.Tag)
            | _ -> invalidArg (string o) "Invalid comparison for State"
    
    
type ActiveState<'T> =
    {
        State: State<'T>
        BehaviorQueue: Queue<BehaviorTree.Node<'T>>
        Status: BehaviorTree.Status
    }
    static member Create state = {
        State = state
        BehaviorQueue = Queue.empty
        Status = BehaviorTree.Running
    }
type TransitionsMap<'T> = Map<State<'T>,(State<'T> * ('T -> BehaviorTree.Status -> bool)) []>
let FindTransition data status (state: State<'T>, condition: 'T -> BehaviorTree.Status -> bool) =
    state.Condition data && condition data status
    
let Tick (transitionsMap: TransitionsMap<'T>) (activeState: ActiveState<'T>) data =
    let transitions = match transitionsMap.TryFind activeState.State with
                        | Some (t) -> t
                        | None -> [||]
    let next = match Array.tryFind (FindTransition data activeState.Status) transitions with
                | Some(state, _) ->
                    printfn "Entering state %s" state.Tag
                    state.OnEnter data
                    ActiveState<'T>.Create state                    
                | None -> activeState
    let q = if next.BehaviorQueue.IsEmpty then
                BehaviorTree.InitTree next.State.BehaviorRoot
            else next.BehaviorQueue
    let (queue, status) = BehaviorTree.Tick q data
    { next with
        BehaviorQueue = queue
        Status = status
    }

let InvalidState: State<obj> =
    {
        Tag = "InvalidState"
        Condition = fun _ -> invalidOp "Called function on InvalidState"
        OnEnter = fun _ -> invalidOp "Called function on InvalidState"
        BehaviorRoot = invalidOp "Called function on InvalidState"
    }
