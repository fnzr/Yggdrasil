module Yggdrasil.Behavior.StateMachine

open FSharpx.Collections

type Transition<'T> = {
    Condition: 'T -> bool
    OnTransit: ('T -> unit) option
    State: State<'T>
}
and State<'T> = {
    Condition: 'T -> bool
    Transitions: Transition<'T>[]
    BehaviorRoot: BehaviorTree.Factory<'T>
}
and ActiveState<'T> =
    {
        State: State<'T>
        BehaviorQueue: Queue<BehaviorTree.Node<'T>>
    }
    static member Create state = {
        State = state
        BehaviorQueue = BehaviorTree.InitTree state.BehaviorRoot
    }    

let FindTransition data (transition: Transition<'T>) =
    transition.Condition data && transition.State.Condition data
let Tick (activeState: ActiveState<'T>) data =
    let next = match Array.tryFind (FindTransition data) activeState.State.Transitions with
                | Some(t) -> ActiveState<'T>.Create t.State
                | None -> activeState
    let (queue, status) = BehaviorTree.Tick next.BehaviorQueue data
    
    if status = BehaviorTree.Running
        then {activeState with BehaviorQueue = queue}
    else ActiveState<'T>.Create activeState.State

(*
let IdleState = {
    Condition = fun _ -> true
    Transitions = [||]
    BehaviorRoot = BehaviorTree.Action IncreaseSuccessNode
}
*)