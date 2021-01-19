module Yggdrasil.Behavior.StateMachine

open System
open System.Text
open Microsoft.FSharp.Reflection
open NLog

let Logger = LogManager.GetLogger("StateMachine")

let BuildUnionKey union =
    let sb = StringBuilder()
    let rec _build u =
        try
            let (s, os) = FSharpValue.GetUnionFields(u, u.GetType())
            sb.Append(s.Name).Append(".") |> ignore
            _build os.[0]
        with
        | :? ArgumentException
        | :? IndexOutOfRangeException -> sb.Remove(sb.Length - 1, 1).ToString()
    _build union

type Transition<'state, 'data> = {
    Event: string
    Guard: 'data -> bool
    Next: 'state
}

[<CustomEquality; NoComparison>]
type MachineState<'state, 'data, 'blackboard when 'state:equality> =
    {
        State: 'state
        Enter: 'data -> unit
        Exit: 'data -> unit
        Parent: 'state option
        Parents: MachineState<'state, 'data, 'blackboard> list
        Transitions: Map<string, Transition<'state, 'data>>
        AutoTransition: 'state option
        Behavior: ('blackboard -> BehaviorTree.ActiveNode<'data, 'blackboard>  * 'blackboard) option
    }
    override x.GetHashCode () = x.State.GetHashCode ()
    override x.Equals o =
        match o with
        | :? MachineState<'state, 'data, 'blackboard> as y -> x.State = y.State
        | _ -> false
        

let configure state = {
    State = state
    Enter = fun _ -> ()
    Exit = fun _ -> ()
    Parent = None
    Parents = List.empty
    Transitions = Map.empty
    AutoTransition = None
    Behavior = None
}

let withParent parent state = {state with Parent = Some(parent)}
let onExit f state = {state with Exit = f}
let onEnter f state = {state with Enter = f}
let transitTo otherState state = {state with AutoTransition = Some(otherState)}
let on event endState state =
    let key = BuildUnionKey event
    let trans = {Event = key; Guard = (fun _ -> true); Next = endState}
    {state with Transitions = state.Transitions.Add(key, trans)}
let onIf event guard endState state =
    let key = BuildUnionKey event
    let trans = {Event = key; Guard = guard; Next = endState}
    {state with Transitions = state.Transitions.Add(key, trans)}
let withBehavior factory state = {state with Behavior = Some(factory)}

//Exists in s1 but not s2
let rec DivergentPath (s1: 'a list) (s2: 'a list) =
    if s1.IsEmpty then s2
    elif s2.IsEmpty then []
    else
        match List.tryFindIndex (fun e -> e = s2.Head) s1 with
        | None -> DivergentPath s1 s2.Tail
        | Some i -> s1.[..i-1]        
        
let FindMachineState (machineStates: MachineState<'state, 'data, 'blackboard> list)
    state = List.find (fun ms -> ms.State = state) machineStates
    
let rec FindAcceptableTransition state event data =
    let chooseTransition candidate =        
        match candidate.Transitions.TryFind event with
            | Some t -> if t.Guard data then Some(t) else None
            | None -> None
    List.tryPick chooseTransition (state :: state.Parents)
    
type StateMachine<'state, 'data, 'blackboard when 'state:equality> =
    {
        States: MachineState<'state, 'data, 'blackboard> list
        CurrentState: MachineState<'state, 'data, 'blackboard>
    }
    member this.Start data =
        List.iter (fun s -> s.Enter data) this.CurrentState.Parents
        this.CurrentState.Enter data
    member this.TransitionTo state data =
        let nextState = FindMachineState this.States state
        let exitStates = DivergentPath this.CurrentState.Parents nextState.Parents
        List.iter (fun s -> s.Exit data) exitStates
        
        let enterStates = DivergentPath nextState.Parents this.CurrentState.Parents
        List.iter (fun s -> s.Enter data) enterStates
        
        nextState.Enter data
        
        Logger.Info ("{oldState} => {newState}", this.CurrentState.State, state)
        
        match nextState.AutoTransition with
        | Some s -> this.TransitionTo s data
        | None -> {this with CurrentState = nextState} 
        
    member this.TryTransit event data =        
        match FindAcceptableTransition this.CurrentState event data with
        | Some t -> Some <| this.TransitionTo t.Next data
        | None -> None

let CreateStateMachine machineStates initialState =    
    let rec GetParents machineState =
        match machineState.Parent with
            | Some p ->
                let parentState = FindMachineState machineStates p
                (parentState)::(GetParents parentState)
            | None -> []
    let compiledStates = List.map
                             (fun ms -> {ms with Parents = GetParents ms |> List.rev}) machineStates
    let initial = FindMachineState machineStates initialState
    {States = compiledStates; CurrentState = initial}
