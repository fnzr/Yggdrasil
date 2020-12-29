module Yggdrasil.Behavior.Machines

open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Behavior.Trees
open Yggdrasil.Agent
let Logger = LogManager.GetLogger("Machines")

let DefaultState = {
        Tag = "InvalidState"
        Condition = fun _ -> true
        OnEnter = fun _ -> ()
        OnLeave = fun _ -> ()
        Behavior = Action (fun _ -> invalidOp "Invalid Node")
    }

let InitialState =
    { DefaultState with
        Tag = "InitialState"
        Behavior = IsConnected
        OnLeave = fun agent ->
            agent.Dispatcher DoneLoadingMap
    }

let IdleState =
    { DefaultState with
        Tag = "IdleState"
        Behavior = Wait 5000L
    }

let WalkNorthState =
    { DefaultState with
        Tag = "WalkNorthState"
        Behavior = Walk
        OnEnter =
            fun agent ->
                let (x, y) = agent.Position
                agent.Goals.Position <- Some(x, y + 5)
    }
let WalkSouthState =
    { DefaultState with
        Tag = "WalkSouthState"
        Behavior = Walk
        OnEnter = fun agent ->
            let (x, y) = agent.Position
            agent.Goals.Position <- Some(x, y - 5)
    }

let InitStateTransitions: Transition<Agent>[] = [|
    IdleState, AgentEvent.ConnectionStatusChanged, fun agent -> agent.IsConnected
|]

let IStateTransitions: Transition<Agent>[] = [|
    WalkNorthState, AgentEvent.BTStatusChanged, fun agent -> agent.MachineState.Status = Success
|]

let WNorthTransitions: Transition<Agent>[] = [|
    WalkSouthState, AgentEvent.BTStatusChanged, fun agent -> agent.MachineState.Status = Success
|]

let WSouthTransitions: Transition<Agent>[] = [|
    IdleState, AgentEvent.BTStatusChanged, fun agent -> agent.MachineState.Status = Success
|]

let TMap =
    Map.empty
        .Add(IdleState, IStateTransitions)
        .Add(WalkNorthState, WNorthTransitions)
        .Add(WalkSouthState, WSouthTransitions)
        .Add(InitialState, InitStateTransitions)

type Transitions = (MachineState<Agent> * (Agent -> Status -> bool))
let InitialStateTransitions: Transitions[]  = [|
    IdleState, fun agent _ -> agent.IsConnected
|]

let IdleTransitions: Transitions[]  = [|
    WalkNorthState, fun _ status -> status = Success
|]

let WalkNorthTransitions: Transitions[]  = [|
    WalkSouthState, fun _ status -> status = Success
|]

let WalkSouthTransitions: Transitions[]  = [|
    IdleState, fun _ status -> status = Success
|]

let TransitionsMap =
    Map.empty
        .Add(IdleState, IdleTransitions)
        .Add(WalkNorthState, WalkNorthTransitions)
        .Add(WalkSouthState, WalkSouthTransitions)
        .Add(InitialState, InitialStateTransitions)