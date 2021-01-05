module Yggdrasil.Behavior.Machines

open System.Net
open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.IO
open Yggdrasil.Types
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Agent
let Logger = LogManager.GetLogger("Machines")

let WalkNorth (agent: Agent) =
    let (x, y) = agent.Location.Position
    agent.Goals.Position <- Some(x, y + 5)
    
let WalkSouth (agent: Agent) =
    let (x, y) = agent.Location.Position
    agent.Goals.Position <- Some(x, y - 5)

let DefaultMachine server username password = 
    let states = [
        configure Terminated
            |> onEnter (fun (a: Agent) -> Logger.Warn("Agent disconnected: {name}", a.Name))
        configure Disconnected
            |> onEnter (Handshake.Login server username password)
            |> on ConnectionAccepted Connected
        configure Connected
            |> transitTo Idle
            |> on ConnectionTerminated Terminated
        configure Idle
            |> withParent Connected
            |> onEnter (fun (a: Agent) -> a.DelayPing 3000.0)
            |> on Ping WalkingNorth
        configure WalkingNorth
            |> withParent Connected
            |> withBehavior Trees.Walk
            |> onEnter WalkNorth
            |> on BehaviorTreeSuccess WalkingSouth
        configure WalkingSouth
            |> withParent Connected
            |> withBehavior Trees.Walk
            |> onEnter WalkSouth
            |> on BehaviorTreeSuccess Idle
    ]
    CreateStateMachine states Disconnected
