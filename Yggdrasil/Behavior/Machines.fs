module Yggdrasil.Behavior.Machines

open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.IO
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Agent.Agent
open Yggdrasil.Agent.Event
let Logger = LogManager.GetLogger("Machines")


let WalkNorth (agent: Agent) =
    let (x, y) = agent.Location.Position
    agent.Goals.Position <- Some(x, y - 21)
    
let WalkSouth (agent: Agent) =
    let (x, y) = agent.Location.Position
    agent.Goals.Position <- Some(x, y + 21)

module DefaultMachine =
    open Yggdrasil.Behavior
    
    type State =
        | Terminated
        | Disconnected
        | Connected
        | WalkingNorth
        | Idle
        | WalkingSouth

    let Create server username password =
        let states = [
            configure State.Terminated
                |> onEnter (fun (a: Agent) -> Logger.Warn ("Agent disconnected: {name}", a.Name))
            configure State.Disconnected
                |> onEnter (Handshake.Login server username password)
                |> on (Connection Active) State.Connected
            configure State.Connected
                |> transitTo Idle
                |> on (Connection Inactive) State.Terminated
            configure State.WalkingNorth
                |> withParent State.Connected
                |> withBehavior (BuildTree (Trees.Walk))
                |> onEnter (WalkNorth)
                |> on (BehaviorResult Success) State.Idle
            configure State.Idle
                |> withParent State.Connected
                |> withBehavior (BuildTree (Trees.Wait 3000L))
                |> on (BehaviorResult Success) State.WalkingSouth
            configure State.WalkingSouth
                |> withParent State.Connected
                |> withBehavior (BuildTree (Trees.Walk))
                |> onEnter (WalkSouth)
                |> on (BehaviorResult Success) State.WalkingNorth
        ]
        Yggdrasil.Behavior.StateMachine.CreateStateMachine states Disconnected
