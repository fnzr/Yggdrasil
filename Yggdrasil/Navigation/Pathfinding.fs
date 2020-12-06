module Yggdrasil.Navigation.Pathfinding

open System
open FSharpx.Collections
open Yggdrasil.Navigation.Maps

[<CustomEquality; CustomComparison>]
type Point =
    {X: int16; Y: int16}
    
    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? Point as o ->
                if this.Y = o.Y then this.X.CompareTo(o.X);
                else this.Y.CompareTo(o.Y);
            | _ -> invalidArg "other" "Invalid comparison for Node"
    override this.Equals other =
        match other with
        | :? Point as o -> this.X = o.X && this.Y = o.Y
        | _ -> invalidArg "other" "Invalid equality for Point"
        
    override this.GetHashCode() = (this.X * this.Y).GetHashCode()


type Node(point: Point, partialHeuristic: Point -> int16) =
    member public this.Value = partialHeuristic point
    member public this.Point = point
    
    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? Node as o -> this.Value.CompareTo(o.Value)
            | _ -> invalidArg "other" "Invalid comparison for Node"
            
    override this.Equals other =
        match other with
        | :? Node as o -> this.Point = o.Point
        | _ -> invalidArg "other" "Invalid equality for Node"
        
    override this.GetHashCode() = this.Point.GetHashCode() 
            
let ManhattanDistance a b = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y)

let getScore (map: Map<Node, int16>) node =
    match map.TryFind node with
    | None -> Int16.MaxValue
    | Some(score) -> score
    
let TryGetPoint (map: MapData) heuristic point =
    let index = int (point.X * point.Y)
    if index < map.Cells.Length && map.Cells.[index].HasFlag(CellType.WALK)
    then Some(Node(point, heuristic))
    else None
        
let Neighbours (map: MapData) heuristic (origin: Point) =
    Array.choose (fun optNode -> optNode)
        [|
            TryGetPoint map heuristic {X=origin.X+1s;Y=origin.Y}; //north
            TryGetPoint map heuristic {X=origin.X-1s;Y=origin.Y}; //south
            TryGetPoint map heuristic {X=origin.X;Y=origin.Y-1s}; //west
            TryGetPoint map heuristic {X=origin.X;Y=origin.Y+1s}; //east
        |]    

let rec Step map goal heuristic ((queue: Set<Node>), (path: Map<Node, Node>), (gScores: Map<Node, int16>), (fScores: Map<Node, int16>)) =
    if queue.IsEmpty then Map.empty
    else
        let current = queue.MinimumElement
        if goal = current.Point then path //found
        else
            let tentative = getScore gScores current + 1s //weight of the edge
            let folder ((q: Set<Node>), (p: Map<Node,Node>),
                        (g: Map<Node, int16>), (f: Map<Node, int16>)) (n: Node) =
                if tentative < getScore gScores n then
                    (if q.Contains n then q else q.Add n),
                    p.Add(n, current),
                    g.Add(n, tentative),
                    f.Add(n, tentative + n.Value)
                else q, p, g, f                    
            let state = queue, path, gScores, fScores
            Step map goal heuristic <| Array.fold folder state (Neighbours map heuristic current.Point)
            
let rec ReconstructPath (path: Map<Node, Node>) node =
    if path.ContainsKey node then
        //printfn "%A" node.Point
        ReconstructPath path <| path.[node]
    else ()
        
let AStar map start goal heuristic =
    let h = heuristic goal
    let node =Node(start, h)
    let queue = Set.empty.Add(node)
    
    let path = Map.empty
    
    let gScores = Map.empty.Add(node, 0s)
    let fScores = Map.empty.Add(node, h start)
    
    let result = Step map goal h (queue, path, gScores, fScores)
    if result.IsEmpty then printfn "No path found"
    else ReconstructPath result <| Node(goal, h)
    