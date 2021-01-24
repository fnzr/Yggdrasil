module Sunna.Machines

open NLog
open FSharpPlus.Lens
open Yggdrasil.Behavior.FSM.Machine
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.Game
open Yggdrasil.Game.Event

let Logger = LogManager.GetLogger "Machines"

let IsConnected world = world.IsConnected
let IsDisconnected world = not <| IsConnected world
let PlayerIs status world = world.Player.Unit.Status = status

let IsReady world = world.IsMapReady

let WalkNorth world =
    let (x, y) = world.Player.Position
    let goal = Some(x + 5, y)
    setl World._Player <|
        setl Player._Goals {world.Player.Goals with Position = goal} world.Player
    <| world
    
let WalkSouth world =
    let (x, y) = world.Player.Position
    let goal = Some(x - 5, y)
    setl World._Player <|
        setl Player._Goals {world.Player.Goals with Position = goal} world.Player
    <| world
    
let Login world = world.Login world; world
    
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
                |> behavior (Trees.Login NoOp)
                |> on IsConnected Connected
            configure Connected
                |> auto Idle
                |> on IsDisconnected Terminated
            configure WalkingNorth
                |> enter WalkNorth
                |> behavior (Trees.Walk DefaultRoot)
                |> parent Connected
                |> behaviorSuccess WalkingSouth
            configure WalkingSouth
                |> enter WalkSouth
                |> behavior (Trees.Walk DefaultRoot)
                |> parent Connected
                |> behaviorSuccess Idle
            configure Idle
                |> parent Connected
                |> behavior (Trees.Wait 1000L DefaultRoot)
                |> behaviorSuccess WalkingNorth
        ]
        CreateMachine states Disconnected
