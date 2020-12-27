module Yggdrasil.Behavior.Machines

open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Types
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Behavior.Trees
let IdleState = {
    Tag = "IdleState"
    Condition = fun _ -> true    
    BehaviorRoot = IsConnected
    OnEnter =  fun _ -> ()
}
let WalkState = {
    Tag = "WalkState"
    Condition = fun _ -> true
    BehaviorRoot = Walk
    OnEnter = fun agent ->
        let (x, y) = agent.Position
        agent.Goals.Position <- Some(x, y + 5)
}

let WalkNorthState = {
    Tag = "WalkNorthState"
    Condition = fun _ -> true
    BehaviorRoot = Walk
    OnEnter = fun agent ->
        let (x, y) = agent.Position
        agent.Goals.Position <- Some(x, y + 5)
}
let WalkSouthState = {
    Tag = "WalkSouthState"
    Condition = fun _ -> true
    BehaviorRoot = Walk
    OnEnter = fun agent ->
        let (x, y) = agent.Position
        agent.Goals.Position <- Some(x, y - 5)
}

let StandingState = {
    Tag = "StandingState"
    Condition = fun _ -> true
    BehaviorRoot = Wait 5000L
    OnEnter = fun _ -> ()
}

let StopState = {
    Tag = "StopState"
    Condition = fun (_: Agent) -> true
    BehaviorRoot = Action (fun _ -> Status.Success)
    OnEnter = fun _ -> ()
}

let IdleTransitions: (State<Agent> * (Agent -> Status -> bool))[]  = [|
    //WalkState, fun agent _ -> agent.IsConnected
    WalkNorthState, fun agent _ -> agent.IsConnected
|]

let WalkTransitions: (State<Agent> * (Agent -> Status -> bool))[]  = [|
    StopState, fun agent _ -> agent.Destination = None
|]

let WalkNorthTransitions: (State<Agent> * (Agent -> Status -> bool))[]  = [|
    WalkSouthState, fun _ status -> status = Success
|]

let StandingTransitions: (State<Agent> * (Agent -> Status -> bool))[]  = [|
    WalkNorthState, fun _ status -> status = Success
|]

let WalkSouthTransitions: (State<Agent> * (Agent -> Status -> bool))[]  = [|
    StandingState, fun _ status -> status = Success
|]

let TransitionsMap =
    Map.empty
        .Add(IdleState, IdleTransitions)
        .Add(WalkState, WalkTransitions)
        .Add(WalkNorthState, WalkNorthTransitions)
        .Add(WalkSouthState, WalkSouthTransitions)
        .Add(StandingState, StandingTransitions)