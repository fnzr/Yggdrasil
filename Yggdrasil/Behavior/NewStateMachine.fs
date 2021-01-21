module Yggdrasil.Behavior.FSM

open Yggdrasil.Behavior.BehaviorTree

type State<'state, 'data> = {
    Name: 'state
    Parent: 'state option
    AutoTransition: 'state option
    Behavior: ActiveNode<'data>
}

type Transition<'state, 'data> = {
    Guard: 'data -> bool
    State: 'state
}

type Machine<'state, 'event, 'data when 'state: comparison and 'event:comparison> =
    {
        Transitions: Map<('state * 'event), Transition<'state, 'data>>
        States: Map<'state, State<'state, 'data>>
    }
    static member Default = {Transitions=Map.empty;States=Map.empty}

module State =
    let rec Enter machine state =
        let _state = machine.States.[state]
        match _state.AutoTransition with
        | Some s -> Enter machine s
        | None -> _state
        
    let rec TryEnter machine event data state =
        match machine.Transitions.TryFind (state.Name, event) with
        | None ->
            match state.Parent with
            | Some p -> TryEnter machine event data (machine.States.[p])
            | None -> None
        | Some t -> if t.Guard data then Some <| Enter machine t.State else None
        
    let Handle machine event data state =
        match TryEnter machine event data state with
        | Some s -> s
        | None -> state
        
    let Tick machine data state =
        match state.Behavior data with
        | End s -> machine.States.[state.Name], Some s
        | Next n -> {state with Behavior = n}, None
        
    let Configure state = {
        Name = state
        Parent = None
        AutoTransition = None
        Behavior = NoOp
    }
    
    let withParent parent state = {state with Parent = Some parent}
    let auto autoEnter state = {state with AutoTransition = Some autoEnter}
    let behavior root state = {state with Behavior = root}
    
module Machine =
    
    let transition exit event enter machine =
        {machine with Transitions=machine.Transitions.Add((exit, event), enter)}