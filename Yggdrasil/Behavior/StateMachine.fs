module Yggdrasil.Behavior.FSM

open NLog
open Yggdrasil.Behavior.BehaviorTree

let Logger = LogManager.GetLogger "StateMachine"

type State<'data, 'stateId, 'event
    when 'event:comparison and 'stateId:comparison> = {
    Id: 'stateId
    Auto: 'stateId option
    Parent: 'stateId option    
    Behavior: ActiveNode<'data>
    Events: Map<'event, 'stateId>
    Monitors: ('data -> 'stateId option) list
}

type ActiveState<'data, 'stateId, 'event
    when 'event:comparison and 'stateId:comparison> = {
    Base: State<'data, 'stateId, 'event>
    Behavior: ActiveNode<'data>
    StateMap: Map<'stateId, State<'data, 'stateId, 'event>>
    BehaviorResultEvent: Status -> 'event
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
        
    let rec TryEnter event data (stateMap: Map<_, _>) state =
        match state.Events.TryFind event with
        | None ->
            match state.Parent with
            | Some p -> TryEnter event data stateMap stateMap.[p]
            | None -> None
        | Some id ->
            Logger.Debug ("{stateOut} => {stateIn}", state.Id, id)
            Some <| Enter stateMap id
        
    let Handle event data activeState = 
        match TryEnter event data activeState.StateMap activeState.Base with
        | Some s -> EnterActiveState s activeState
        | None -> activeState
        
    let Tick data activeState =
        match activeState.Behavior data with
        | End e -> 
            let next = Handle (activeState.BehaviorResultEvent e) data activeState
            if LanguagePrimitives.PhysicalEquality next activeState then
                {activeState with Behavior=activeState.Base.Behavior}
            else next
        | Next n -> {activeState with Behavior = n}
        
    let Monitor data activeState =
        match List.tryPick (fun m -> m data) activeState.Base.Monitors with
        | Some id ->
            EnterActiveState (Enter activeState.StateMap id) activeState
        | None -> activeState

module Machine =
    let configure stateId = {
        Id = stateId
        Auto = None
        Parent = None
        Behavior = NoOp
        Events = Map.empty
        Monitors = []
    }
    let _true = fun _ -> true
    
    let on event outState state =
        {state with Events=state.Events.Add(event, outState)}

    let parent parent state = {state with Parent=Some parent}
        
    let auto autoState state = {state with Auto=Some autoState}
    
    let behavior root (state: State<_,_,_>) = {state with Behavior=root}
    
    let monitor fn state = {state with Monitors=fn :: state.Monitors}
    
    let CreateMachine states initialState behaviorResultConverter =
        let map = List.fold (fun (m: Map<_,_>) s -> m.Add(s.Id, s)) Map.empty states
        let state = map.[initialState]
        {
            Base = state
            Behavior = state.Behavior
            StateMap = map
            BehaviorResultEvent = behaviorResultConverter
        } 