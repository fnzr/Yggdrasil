module Yggdrasil.Behavior.Machines

open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game
open Yggdrasil.IO
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Game.Event
let Logger = LogManager.GetLogger "Machines"

let WalkNorth (world: World) =
    let (x, y) = world.Player.Position
    world.Player.Goals.Position <- Some(x - 5, y)
    
let WalkSouth (world: World) =
    let (x, y) = world.Player.Position
    world.Player.Goals.Position <- Some(x + 5, y)

module DefaultMachine =
    open Yggdrasil.Behavior
    
    type State =
        | Terminated
        | Disconnected
        | Connected
        | WalkingNorth
        | Idle
        | WalkingSouth
    let Create server username password callback =
        let states = [
            configure State.Terminated
                |> onEnter (fun (w: World) -> Logger.Warn ("Agent disconnected: {name}", w.Player.Name))
            configure State.Disconnected
                |> onEnter (fun (_: World) -> Handshake.Login server username password callback)
                |> on (ConnectionStatus Active) State.Connected
            configure State.Connected
                |> transitTo Idle
                |> on (ConnectionStatus Inactive) State.Terminated
            configure State.WalkingNorth
                |> withParent State.Connected
                |> withBehavior (Trees.Walk RootComplete) 
                |> onEnter (WalkNorth)
                |> on (BehaviorResult Success) State.Idle
            configure State.Idle
                |> withParent State.Connected
                |> withBehavior (Trees.Wait 3000L RootComplete)
                |> on (BehaviorResult Success) State.WalkingSouth
            configure State.WalkingSouth
                |> withParent State.Connected
                |> withBehavior (Trees.Walk RootComplete)
                |> onEnter (WalkSouth)
                |> on (BehaviorResult Success) State.WalkingNorth
        ]
        Yggdrasil.Behavior.StateMachine.CreateStateMachine states Disconnected
