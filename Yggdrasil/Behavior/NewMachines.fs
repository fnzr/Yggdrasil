module Yggdrasil.Behavior.NewMachines

open NLog
open Yggdrasil.Behavior.FSM
open Yggdrasil.Behavior.FSM.State
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game

let Logger = LogManager.GetLogger "Machines"

let WalkNorth (world: World) =
    let (x, y) = world.Player.Position
    world.Player.Goals.Position <- Some(x - 5, y)
    
let WalkSouth (world: World) =
    let (x, y) = world.Player.Position
    world.Player.Goals.Position <- Some(x - 5, y)

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
            Configure Terminated
            Configure Disconnected
            Configure Connected
                |> auto Idle
            Configure WalkingNorth
                |> behavior (Trees.Walk DefaultRoot)
                |> withParent Connected
            Configure WalkingSouth
                |> behavior (Trees.Walk DefaultRoot)
                |> withParent Connected
        ]
        states
        //Yggdrasil.Behavior.StateMachine.CreateStateMachine states Disconnected
