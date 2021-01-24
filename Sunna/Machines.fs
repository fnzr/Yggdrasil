module Sunna.Machines

open NLog
open Yggdrasil.Behavior.FSM.Machine
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game
open Yggdrasil.Game.Event

let Logger = LogManager.GetLogger "Machines"

let IsConnected world = world.IsConnected
let IsDisconnected world = not <| IsConnected world

let PlayerIs status world = world.Player.Unit.Status = status
    

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
                |> on IsConnected Connected
                |> behavior (Trees.Login NoOp)
            configure Connected
                |> auto Idle
                |> on IsDisconnected Terminated
            configure WalkingNorth
                |> behavior (Trees.WalkNorth DefaultRoot)
                |> parent Connected
                |> on (PlayerIs Yggdrasil.Game.Idle) WalkingSouth
            configure WalkingSouth
                |> behavior (Trees.WalkSouth DefaultRoot)
                |> parent Connected
                //|> on (BehaviorResult Success) Idle
            configure Idle
                |> parent Connected
                |> behavior (Trees.Wait 3000L DefaultRoot)
                //|> on (BehaviorResult Success) WalkingNorth
        ]
        CreateMachine states Disconnected
