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
        Behavior = Wait 5000.0
    }

let WalkNorthState =
    { DefaultState with
        Tag = "WalkNorthState"
        Behavior = Walk
        OnEnter =
            fun agent ->
                let (x, y) = agent.Location.Position
                agent.Goals.Position <- Some(x, y + 5)
    }
let WalkSouthState =
    { DefaultState with
        Tag = "WalkSouthState"
        Behavior = Walk
        OnEnter = fun agent ->
            let (x, y) = agent.Location.Position
            agent.Goals.Position <- Some(x, y - 5)
    }

let InitialStateTransitions: Transition<Agent>[] = [|
    IdleState, AgentEvent.ConnectionStatusChanged, fun agent -> agent.IsConnected
|]

let IdleStateTransitions: Transition<Agent>[] = [|
    WalkNorthState, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
|]

let WalkNorthTransitions: Transition<Agent>[] = [|
    WalkSouthState, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
|]

let WalkSouthTransitions: Transition<Agent>[] = [|
    IdleState, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
|]

let DefaultStateMachine =
    Map.empty
        .Add(IdleState, IdleStateTransitions)
        .Add(WalkNorthState, WalkNorthTransitions)
        .Add(WalkSouthState, WalkSouthTransitions)
        .Add(InitialState, InitialStateTransitions)
