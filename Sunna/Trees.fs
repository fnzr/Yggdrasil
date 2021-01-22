module Sunna.Trees

open System
open System.Net
open NLog
open Yggdrasil
open Yggdrasil.Game
open Yggdrasil.Behavior.BehaviorTree
open Yggdrasil.IO

let Logger = LogManager.GetLogger "Trees"

[<Literal>]
let MAX_WALK_DISTANCE = 10

let Walk =    
    let WalkingRequired (world: World) =
        let player = world.Player
        player.Goals.Position.IsSome &&
        Navigation.Pathfinding.DistanceTo
            player.Position player.Goals.Position.Value > 0
    
    let DispatchWalk =
        Stateless <|
        fun world _ -> 
            let player = world.Player
            match player.Goals.Position with
            | Some (x, y) ->
                match Navigation.Pathfinding.FindPath
                      (Navigation.Maps.GetMapData world.Map) player.Position (x, y) 0 with
                | [] -> Result Failure
                | path ->
                    let pos = path
                            |> List.take (Math.Min (MAX_WALK_DISTANCE, path.Length))
                            |> List.last
                    player.Dispatch(Types.Command.RequestMove pos)                
                    Result Success
            | None -> Result Success
    
    let WaitWalkAck = Action {
            State = 0L
            Initialize = fun node -> {node with State = Connection.Tick()}
            Tick = fun world instance ->
                match world.Player.Unit.Status with
                | Idle ->
                    let delay = Connection.Tick() - instance.State                    
                    if delay > 500L then Result Failure
                    else
                        World.RequestPing world 500
                        Node instance
                | Walking -> Result Success
                | _ -> Result Failure
        }
    
    let StoppedWalking =
        Stateless <|
        fun world instance ->
            match world.Player.Unit.Status with        
            | Walking -> Node instance
            | Idle -> Result Success
            | _ -> Result Failure
    
    While WalkingRequired <|
        Sequence [|DispatchWalk; WaitWalkAck; StoppedWalking|]
    (*
    While WalkingRequired
        => (Sequence
            => DispatchWalk
            => WaitWalkAck
            => StoppedWalking)
    *)

let Wait milliseconds =
    Action {
        State = 0L
        Initialize = fun node -> {node with State = Connection.Tick() + milliseconds}
        Tick = fun world instance ->
            let diff = instance.State - Connection.Tick()
            if diff > 0L then
                //Logger.Info diff                
                if not <| world.PingRequested then World.RequestPing world (int diff)
                Node instance
            else Result Success
                
    }
    
let Login =
    Action {
        State = ()
        Initialize = id
        Tick = fun world _ ->
            let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
            let (user, pass) = world.Player.Credentials
            Handshake.Login server user pass world.Inbox
            Result Success
    }
    
let Disconnected =
    Action {
        State = ()
        Initialize = id
        Tick = fun world _ ->
            Logger.Warn ("Disconnected: {name}", world.Player.Name)
            Result Success
    }
    
let WalkNorth =
    let SetNorthGoal =
        Stateless <| fun world _ ->
                     let (x, y) = world.Player.Position
                     world.Player.Goals.Position <- Some(x + 2, y)
                     Result Success
    Sequence [|SetNorthGoal; Walk|]
    
let WalkSouth =
    let SetSouthGoal =
        Stateless <| fun world _ ->
                     let (x, y) = world.Player.Position
                     world.Player.Goals.Position <- Some(x - 2, y)
                     Result Success
    Sequence [|SetSouthGoal; Walk|]
