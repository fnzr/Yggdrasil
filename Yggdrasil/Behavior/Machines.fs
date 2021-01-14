module Yggdrasil.Behavior.Machines

open NLog
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game
open Yggdrasil.IO
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Game.Event
let Logger = LogManager.GetLogger("Machines")

let WalkNorth (game: Game) =
    let (x, y) = game.Player.Position
    game.Player.Goals.Position <- Some(x, y - 1)
    
let WalkSouth (game: Game) =
    let (x, y) = game.Player.Position
    game.Player.Goals.Position <- Some(x, y + 1)

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
                |> onEnter (fun (g: Game) -> Logger.Warn ("Agent disconnected: {name}", g.Player.Name))
            configure State.Disconnected
                |> onEnter (Handshake.Login server username password)
                |> on (ConnectionStatus Active) State.Connected
            configure State.Connected
                |> transitTo Idle
                |> on (ConnectionStatus Inactive) State.Terminated
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
