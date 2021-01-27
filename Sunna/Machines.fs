module Sunna.Machines

open NLog
open FSharpPlus.Lens
open Yggdrasil.Behavior.FSM.Machine
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game

let Logger = LogManager.GetLogger "Machines"

let IsConnected game = game.IsConnected
let IsDisconnected game = game |> IsConnected |> not
let WalkNorth (game: Game) =
    let (x, y) = game.Player.Position
    let goal = Some(x + 5s, y)
    setl Game._Goals {game.Goals with Position = goal} game
    
let WalkSouth (game: Game) =
    let (x, y) = game.Player.Position
    let goal = Some(x - 5s, y)
    setl Game._Goals {game.Goals with Position = goal} game
    
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
                |> behavior (Trees.Disconnected NoOpRoot)
            configure Disconnected
                |> behavior (Trees.Login NoOpRoot)
                |> on IsConnected Connected
            configure Connected
                |> auto Idle
                |> on IsDisconnected Terminated
            configure WalkingNorth
                |> enter WalkNorth
                |> behavior (Trees.Walk StatusRoot)
                |> parent Connected
                |> behaviorSuccess WalkingSouth
            configure WalkingSouth
                |> enter WalkSouth
                |> behavior (Trees.Walk StatusRoot)
                |> parent Connected
                |> behaviorSuccess Idle
            configure Idle
                |> parent Connected
                |> behavior (Trees.Wait 1000L StatusRoot)
                //|> behaviorSuccess WalkingNorth
        ]
        CreateMachine states Disconnected
