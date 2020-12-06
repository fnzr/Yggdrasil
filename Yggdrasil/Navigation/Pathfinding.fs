module Yggdrasil.Navigation.Pathfinding

open System
open System.Collections.Generic
open FSharpx.Collections
open Priority_Queue
open Yggdrasil.Navigation.Maps

//Same as server (also same issue; see path.cpp:path_search)
[<Literal>]
let MAX_WALK_PATH = 32

let ToPoint map index =
    let y, x = Math.DivRem (index, int map.Width)
    x, y

let ToIndex map point = (int map.Width * snd point) + fst point

let ManhattanDistance map a b =
    let x1, y1 = ToPoint map a
    let x2, y2 = ToPoint map b
    Math.Abs(x1 - x2) + Math.Abs(y1 - y2)

let Heuristics map a b = float32 <| ManhattanDistance map a b

type Node(index: int, parent) =
    inherit FastPriorityQueueNode()
    member public this.Index = index
    member public this.Parent = parent    
    interface IComparable with
        member this.CompareTo other = this.Index.CompareTo(other)     
    override this.Equals other = this.Index.Equals(other)
    override this.GetHashCode() = this.Index.GetHashCode()
        
let EnqueueIndex map (queue: FastPriorityQueue<Node>) (visited: Dictionary<int, float32>) goal parent index =    
    if index < map.Cells.Length &&
       map.Cells.[index].HasFlag(CellType.WALK) then
       let cost = Heuristics map goal index
       let oldCost = visited.GetValueOrDefault(index, Single.MaxValue)
       if cost < oldCost then
           visited.[index] <- cost
           let node = Node(index, Some(parent))
           if queue.Contains node
            then queue.UpdatePriority (node, cost)
            else queue.Enqueue (node, cost)

let rec FindPath map (queue: FastPriorityQueue<Node>) (visited: Dictionary<int, float32>) goal =
    if queue.Count = 0 then None
    else
        let node = queue.Dequeue()
        if node.Index = goal then Some(node)
        else
            Array.iter (EnqueueIndex map queue visited goal node)        
                [|
                    node.Index - int map.Width //north
                    node.Index + int map.Width //south
                    node.Index - 1 //west
                    node.Index + 1 //east
                |]
            FindPath map queue visited goal

let rec ReconstructPath map (node: Node) =
    //printfn "%A" <| ToPoint map node.Index
    match node.Parent with
    | None -> ()
    | Some (parent) -> ReconstructPath map parent
let AStar map start goal =
    let queue = FastPriorityQueue<Node>(MAX_WALK_PATH * MAX_WALK_PATH)
    let s = ToIndex map start
    let g = ToIndex map goal
    
    queue.Enqueue (Node(s, None), 0.0f)
    let result = FindPath map queue (Dictionary()) g
    match result with
    | Some(node) -> ReconstructPath map node
    | None -> printfn "Path not found"