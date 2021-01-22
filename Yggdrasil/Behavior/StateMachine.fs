module Yggdrasil.Behavior.FSM

open NLog
open Yggdrasil.Behavior.BehaviorTree

let Logger = LogManager.GetLogger "StateMachine"

type Transition<'data, 'stateId> = {
    Guard: 'data -> bool
    State: 'stateId
}

type State<'data, 'stateId, 'event
    when 'event:comparison and 'stateId:comparison> = {
    Id: 'stateId
    Auto: 'stateId option
    Parent: 'stateId option    
    Behavior: ActiveNode<'data>
    Transitions: Map<'event, Transition<'data, 'stateId>>
}

type ActiveState<'data, 'stateId, 'event
    when 'event:comparison and 'stateId:comparison> = {
    Base: State<'data, 'stateId, 'event>
    Behavior: ActiveNode<'data>
    StateMap: Map<'stateId, State<'data, 'stateId, 'event>>
    BehaviorEvent: Status -> 'event
}

module State =
    (*
    //Exists in s1 but not s2
    let rec DivergentPath (s1: 'a list) (s2: 'a list) =
    if s1.IsEmpty then s2
    elif s2.IsEmpty then []
    else
        match List.tryFindIndex (fun e -> e = s2.Head) s1 with
        | None -> DivergentPath s1 s2.Tail
        | Some i -> s1.[..i-1]
    *)
    let rec Enter (stateMap: Map<_, _>) state =
        let _state = stateMap.[state]
        match _state.Auto with
        | Some s ->
            Logger.Debug ("{stateOut} => {stateIn}", state, s)
            Enter stateMap s
        | None -> _state
        
    let rec TryEnter event data (stateMap: Map<_, _>) state =
        match state.Transitions.TryFind event with
        | None ->
            match state.Parent with
            | Some p -> TryEnter event data stateMap stateMap.[p]
            | None -> None
        | Some t -> if t.Guard data then Some <| Enter stateMap t.State else None
        
    let Handle event data activeState = 
        match TryEnter event data activeState.StateMap activeState.Base with
        | Some s ->
            {Base=s;Behavior=s.Behavior
             StateMap=activeState.StateMap
             BehaviorEvent=activeState.BehaviorEvent}
        | None -> activeState
        
    let rec Tick data activeState =
        match activeState.Behavior data with
        | End e -> 
            let next = Handle (activeState.BehaviorEvent e) data activeState
            if LanguagePrimitives.PhysicalEquality next activeState then
                {activeState with Behavior=activeState.Base.Behavior}
            else next
        | Next n -> {activeState with Behavior = n}

module Machine =
    let configure stateId = {
        Id = stateId
        Auto = None
        Parent = None
        Behavior = NoOp
        Transitions = Map.empty
    }
    let _true = fun _ -> true
    
    let on event outState state =
        {state with Transitions=state.Transitions.Add(event, {Guard=_true; State=outState})}

    let onIf event outState guard state =
        {state with Transitions=state.Transitions.Add(event, {Guard=guard; State=outState})}
        
    let parent parent state = {state with Parent=Some parent}
        
    let auto autoState state = {state with Auto=Some autoState}
    
    let behavior root (state: State<_,_,_>) = {state with Behavior=root}
    
    let CreateMachine states initialState treeResultConverter =
        let map = List.fold (fun (m: Map<_,_>) s -> m.Add(s.Id, s)) Map.empty states
        let state = map.[initialState]
        {
            Base = state
            Behavior = state.Behavior
            StateMap = map
            BehaviorEvent = treeResultConverter
        } 