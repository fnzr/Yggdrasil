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
    
let Tree = Sequence([|Action(CheckCount)
                      Action(IncreaseCounter)
                      Action(IncreaseCounter)
                      Action(CheckCount)
                      Action(IncreaseCounter)
                      Action(CheckCount)|])

let RootFun status = status
let Root = Action(RootFun) :> INode
    
let rec Run tree =
    let state = {Counter = 0}
    let rec run s =
        match s with
        | Action a -> run <| a state
        | Result s -> printfn "Tree finished with status %A" s
    run <| Tree.Step [Root] Status.Initializing state 

[<EntryPoint>]
let main argv =
    Run Tree
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
