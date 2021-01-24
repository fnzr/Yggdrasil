module Yggdrasil.Behavior.FSM

open NLog
open Yggdrasil.Behavior.BehaviorTree

let Logger = LogManager.GetLogger "StateMachine"

type State<'data, 'stateId when 'stateId:comparison> = {
    Id: 'stateId
    Auto: 'stateId option
    Parent: 'stateId option    
    Behavior: ActiveNode<'data>
    Enter: 'data -> 'data
    BehaviorSuccess: 'stateId option
    Transitions: (('data -> bool) * 'stateId) list
}

type ActiveState<'data, 'stateId
    when 'stateId:comparison> = {
    Base: State<'data, 'stateId>
    Behavior: ActiveNode<'data>
    StateMap: Map<'stateId, State<'data, 'stateId>>
}

module State =
    //This static list is built for every transition, this must be a huge f bottleneck
    //consider caching this in the ActiveState or something
    let rec GetParentList (stateMap: Map<_,_>) state =
        let rec _parentList stateId parentList =
            match stateId with
            | None -> parentList
            | Some id ->
                let _state = stateMap.[id]
                _state::(_parentList _state.Parent parentList)
        _parentList state.Parent []
        
    //Exists in s1 but not s2
    let rec DivergentPath (s1: State<_,_> list) (s2: State<_,_> list) =
        if s1.IsEmpty then s2
        elif s2.IsEmpty then []
        else
            match List.tryFindIndex (fun e -> e.Id = s2.Head.Id) s1 with
            | None -> DivergentPath s1 s2.Tail
            | Some i -> s1.[..i-1] 
    let EnterActiveState state activeState =
        {activeState with
            Base=state
            Behavior=state.Behavior}
    let rec Enter (stateMap: Map<_, _>) state data =
        let newState = stateMap.[state]
        let newData = newState.Enter data
        match newState.Auto with
        | Some s ->
            Logger.Debug ("{stateOut} => {stateIn} (auto)", state, s)
            Enter stateMap s newData
        | None -> newData, newState
        
    let ChangeState (stateMap: Map<_,_>) outState inState data =
        let outStateParents = GetParentList stateMap outState
        let inStateParents = GetParentList stateMap inState
        let newParents = DivergentPath inStateParents outStateParents
        let newData = List.fold (fun d s -> s.Enter d) data newParents
        Enter stateMap inState.Id newData
            
    let rec TryTransition (stateMap : Map<_,_>) data state =
        match List.tryPick (fun (c, s) -> if c data then Some s else None) state.Transitions with
        | None ->
            match state.Parent with
            | Some p -> TryTransition stateMap data stateMap.[p]
            | None -> None
        | Some id ->
            Logger.Debug ("{stateOut} => {stateIn}", state.Id, id)
            Some <| ChangeState stateMap state stateMap.[id] data
        
    let MoveState (data, activeState) =
        match TryTransition activeState.StateMap data activeState.Base with
        | None -> data, activeState
        | Some (newData, newState) -> newData, EnterActiveState newState activeState
        
    let Tick (data, activeState) =
        match activeState.Behavior data with
        | End result ->
            if result = Success && activeState.Base.BehaviorSuccess.IsSome then
                let state = activeState.Base
                let inState = activeState.StateMap.[state.BehaviorSuccess.Value]
                let (newData, newState) = ChangeState activeState.StateMap state inState data
                newData, EnterActiveState newState activeState
            else data, {activeState with Behavior = activeState.Base.Behavior} 
        | Next n -> data, {activeState with Behavior = n}
        
module Machine =
    let configure stateId = {
        Id = stateId
        Auto = None
        Parent = None
        BehaviorSuccess = None
        Behavior = NoOp
        Transitions = []
        Enter = id
    }
    let on condition outState state =
        {state with Transitions= (condition, outState) :: state.Transitions}
    let parent parent state = {state with Parent = Some parent}
    let auto autoState state = {state with Auto = Some autoState}
    let behavior root (state: State<_,_>) = {state with Behavior = root}
    let behaviorSuccess outState state = {state with BehaviorSuccess = Some outState}
    let enter onEnter state = {state with Enter = onEnter} 
    
    let CreateMachine states initialState =
        let map = List.fold (fun (m: Map<_,_>) s -> m.Add(s.Id, s)) Map.empty states
        let state = map.[initialState]
        {
            Base = state
            Behavior = state.Behavior
            StateMap = map
        } 