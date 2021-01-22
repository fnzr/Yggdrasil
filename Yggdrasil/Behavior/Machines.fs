module Yggdrasil.Behavior.Machines

open NLog
open Yggdrasil.Behavior.FSM.Machine
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game
open Yggdrasil.Game.Event

let Logger = LogManager.GetLogger "Machines"

module DefaultMachine =
    open Yggdrasil.Behavior
    
    type State =
        | Terminated
        | Disconnected
        | Connected
        | WalkingNorth
        | Idle
        | WalkingSouth
    let Create () =
        let states = [
            configure Terminated
                |> behavior (Trees.Disconnected NoOp)
            configure Disconnected
                |> on (ConnectionStatus Active) Connected
                |> behavior (Trees.Login NoOp)
            configure Connected
                |> auto Idle
                |> on (ConnectionStatus Inactive) Terminated
            configure WalkingNorth
                |> behavior (Trees.WalkNorth DefaultRoot)
                |> parent Connected
                |> on (BehaviorResult Success) WalkingSouth
            configure WalkingSouth
                |> behavior (Trees.WalkSouth DefaultRoot)
                |> parent Connected
                |> on (BehaviorResult Success) Idle
            configure Idle
                |> parent Connected
                |> behavior (Trees.Wait 3000L DefaultRoot)
                |> on (BehaviorResult Success) WalkingNorth
        ]
        let converter (status: BehaviorTree.Status) =
            match status with
            | BehaviorTree.Success -> BehaviorResult Success
            | BehaviorTree.Failure -> BehaviorResult Failure
        CreateMachine states Disconnected converter
