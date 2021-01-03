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
        Behavior = InertAction (fun _ -> invalidOp "Invalid Node")
    }

let InitialState =
    { DefaultState with
        Tag = "InitialState"
        Behavior = IsConnected
        //OnLeave = fun agent -> agent.Dispatcher DoneLoadingMap
    }

let IdleState =
    { DefaultState with
        Tag = "IdleState"
        Behavior = Wait 3000L
        OnEnter = fun agent -> agent.Dispatch Ping
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
    
let GoPronteraField08 =
    { DefaultState with
        Condition = fun agent -> agent.Location.Map.Equals "prontera"
        Tag = "GoPronteraField08"
        Behavior = Walk
        OnEnter = fun agent ->            
            agent.Goals.Position <- Some(156, 22)
    }
    
let GoProntera =
    { DefaultState with
        Condition = fun agent -> agent.Location.Map.Equals "prt_fild08"
        Tag = "GoProntera"
        Behavior = Walk
        OnEnter = fun agent ->            
            agent.Goals.Position <- Some(170, 376)
    }

let InitialStateTransitions: Transition<Agent>[] = [|
    IdleState, AgentEvent.ConnectionStatusChanged, fun agent -> agent.IsConnected
    IdleState, AgentEvent.BTStatusChanged, fun agent -> agent.IsConnected
    IdleState, AgentEvent.MapChanged, fun _ -> true
|]

let MapTransferTransitions: Transition<Agent>[] = [|
    InitialState, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
|]

let IdleStateTransitions: Transition<Agent>[] = [|
    //WalkNorthState, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
    GoPronteraField08, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
    GoProntera, AgentEvent.BTStatusChanged, fun agent -> agent.BTStatus = Success
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
        .Add(GoPronteraField08, MapTransferTransitions)
        .Add(GoProntera, MapTransferTransitions)
