open System
open System.Collections.Generic
open System.Diagnostics
open System.Net
open NLog
open Yggdrasil
open Yggdrasil.BehaviorTree
open Yggdrasil.Navigation
open Yggdrasil.Types

let Logger = LogManager.GetCurrentClassLogger()
(*
type MyState = {
    mutable Counter: int
}

let IncreaseCounter (state: MyState) =
    state.Counter <- state.Counter + 1
    Status.Success

let CheckCount (state: MyState) =
    printfn "Current counter: %A" state.Counter 
    if state.Counter < 3 then Status.Success
    else Status.Success
    
let Stuck _ =
    printfn "Stuck!"
    Status.Running
let Terminate _ =
    printfn "Reached end!"
    Status.Success
    
let HighPriority (state: MyState) =
    printfn "Reached high priority node. Count: %d" state.Counter
    Status.Success

let LowPriority = Sequence([|
                              Action(CheckCount)
                              Action(IncreaseCounter)
                              Action(IncreaseCounter)
                              Action(CheckCount)
                              Action(IncreaseCounter)
                              Action(IncreaseCounter)
                              Action(Stuck)
                              Action(Terminate)|])    
let Switch state = state.Counter > 2  
let ASelector = ActiveSelector(Switch, Action(HighPriority), LowPriority)

let IsConnected (agent: Agent) =
    if agent.IsConnected then Status.Success
    else Status.Failure

let DispatchWalk (agent: Agent) =
    match agent.Goals.Position with
    | Some (x, y) -> 
        agent.Dispatcher(RequestMove (x, y))
        Status.Success
    | None -> Status.Failure
        
let WaitWalkAck (agent: Agent) =
    match agent.Destination with
    | Some(_) -> Status.Success
    | None -> Status.Running
    
let StoppedWalking (agent: Agent) =
    match agent.Destination with
    | Some(_) -> Status.Running
    | None -> Status.Success
    
let WalkNorth (agent: Agent) =
    let (x, y) = agent.Position
    agent.Goals.Position <- Some(x, y - 5)
    Status.Success
    
let WalkSouth (agent: Agent) =
    let (x, y) = agent.Position
    agent.Goals.Position <- Some(x, y + 5)
    Status.Success    
    
let Walk = Sequence[|Action(DispatchWalk);Action(WaitWalkAck);Action(StoppedWalking)|]

let Behavior = Sequence[|Action(IsConnected); Action(WalkNorth); Walk; Action(WalkSouth); Walk|]

let rec Run (tree: Root<'T>) state =    
    let rec run s =
        match s with
        | Action (node, stack, queue) -> run <| NextStep (node, stack, queue) state
        | Result _ -> ()
    run <| tree.Start state
    
let BehaviorFactory name = Root(Behavior)
*)
[<EntryPoint>]
let main argv =
    //Run (Root(ASelector)) {Counter = 0}
    //let map = Yggdrasil.Navigation.Maps.MapCacheParser()    
    //let sw = Stopwatch()
    //sw.Start()
    //Pathfinding.AStar map (150, 90) (150, 85)
    //let p = (150, 90)
    //let i = Pathfinding.ToIndex map p
    //printfn "%A" <| Pathfinding.ToPoint map i
    //printfn "%A" sw.ElapsedMilliseconds
    //let dispatcher = Scheduling.DispatcherFactory()
    //let tick = Scheduling.GetCurrentTick()
    //dispatcher.Post <| (tick+500u, TimedEvent tick)    
    let state = {Counter = 0}
    let latestQ = Run Tree state 
    printfn "%A" state
    //printfn "%A" latestQ.Length
    //printfn "%A" BehaviorParser.behavior
    //printf "Done"
    //let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    //let (agents, login) = API.CreateServerMailboxes loginServer BehaviorFactory
    //login "roboco" "111111"
    //API.CommandLineHandler agents
    //Console.ReadKey() |> ignore 
    0 // return an integer exit code
