module Yggdrasil.Behavior.FSM

open NLog
open Yggdrasil.Behavior.BehaviorTree

let Logger = LogManager.GetLogger "StateMachine"

type State<'data, 'stateId when 'stateId:comparison> = {
    Id: 'stateId
    Auto: 'stateId option
    Parent: 'stateId option    
    Behavior: ActiveNode<'data>
    Transitions: (('data -> bool) * 'stateId) list
}

type ActiveState<'data, 'stateId
    when 'stateId:comparison> = {
    Base: State<'data, 'stateId>
    Behavior: ActiveNode<'data>
    StateMap: Map<'stateId, State<'data, 'stateId>>
}

module State =
    let EnterActiveState state activeState =
        {activeState with
            Base=state
            Behavior=state.Behavior}
    let rec Enter (stateMap: Map<_, _>) state =
        let _state = stateMap.[state]
        match _state.Auto with
        | Some s ->
            Logger.Debug ("{stateOut} => {stateIn} (auto)", state, s)
            Enter stateMap s
        | None -> _state
            
    let rec TryTransition (stateMap : Map<_,_>) data state =
        match List.tryPick (fun (c, s) -> if c data then Some s else None) state.Transitions with
        | None ->
            match state.Parent with
            | Some p -> TryTransition stateMap data stateMap.[p]
            | None -> None
        | Some id ->
            Logger.Debug ("{stateOut} => {stateIn}", state.Id, id)
            Some <| Enter stateMap id
        
    let MoveState data activeState =
        match TryTransition activeState.StateMap data activeState.Base with
        | None -> activeState
        | Some s -> EnterActiveState s activeState
        
    let Tick data activeState =
        match activeState.Behavior data with
        | End _ -> {activeState with Behavior=activeState.Base.Behavior} 
        | Next n -> {activeState with Behavior = n}
        
module Machine =
    let configure stateId = {
        Id = stateId
        Auto = None
        Parent = None
        Behavior = NoOp
        Transitions = []
    }
    let on condition outState state =
        {state with Transitions= (condition, outState) :: state.Transitions}
    let parent parent state = {state with Parent = Some parent}
    let auto autoState state = {state with Auto = Some autoState}
    let behavior root (state: State<_,_>) = {state with Behavior = root}
    
    let CreateMachine states initialState =
        let map = List.fold (fun (m: Map<_,_>) s -> m.Add(s.Id, s)) Map.empty states
        let state = map.[initialState]
        {
            Base = state
            Behavior = state.Behavior
            StateMap = map
        } 