open System
open System.Diagnostics
open System.Net
open NLog
open Yggdrasil
open Yggdrasil.BehaviorTree
open Yggdrasil.Navigation
open Yggdrasil.Types

let Logger = LogManager.GetCurrentClassLogger()

let BehaviorFactory id = ()

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

let rec Run (tree: Root<'T>) state =    
    let rec run s =
        match s with
        | Action (node, stack, queue) -> run <| NextStep (node, stack, queue) state
        | Result s -> printfn "Tree finished with status %A" s
    run <| tree.Start state 

[<EntryPoint>]
let main argv =
    Run (Root(ASelector)) {Counter = 0}
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
    
    //printf "Done"
    //let loginServer = IPEndPoint  (IPAddress.Parse "127.0.0.1", 6900)
    //let (agents, login) = API.CreateServerMailboxes loginServer BehaviorFactory
    //login "roboco" "111111"
    //API.CommandLineHandler agents
    //Console.ReadKey() |> ignore 
    0 // return an integer exit code
