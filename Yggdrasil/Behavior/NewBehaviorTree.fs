open System

type Status = Success | Failure | Running
(*
[<Flags>]
type ParallelFlag =
    OneSuccess = 1 | OneFailure = 2 | AllSuccess = 4 | AllFailure = 8
*)
(*
type TickResult<'Data, 'Key when 'Key: comparison> =
    | Status of Status
    | Update of Map<'Key, obj>
    | Next of BuiltNode<'Data, 'Key> * Map<'Key, obj>
    
and Node<'Data, 'Key when 'Key: comparison> =
    {
        Tick: 'Data -> Map<'Key, obj> -> TickResult<'Data, 'Key>
    }
    static member Default = {
        Tick = fun _ _ -> Status Success
    }

and BuiltNode<'Data, 'Key when 'Key: comparison> = {
    Node: Node<'Data, 'Key>
    OnComplete: Status -> Map<'Key, obj> -> TickResult<'Data, 'Key>
}

let Tick data blackboard activeNode =
    let node, status, bb =
        match activeNode.Node.Tick data blackboard with
        | Status s -> activeNode, s, blackboard
        | Update bb -> activeNode, Running, bb
        | Next (n, bb) -> n, Running, bb 
    match status with    
        | Success | Failure -> activeNode.OnComplete status bb
        | Running -> Next (activeNode, bb)
    
let rec AllTicks activeNode data blackboard =
    match Tick activeNode data blackboard with
    | Status result -> result
    | Next (nextNode, bb) -> AllTicks data bb nextNode
    
let Sequence (children: _[]) =
    let onComplete sibling status bb =
        match status with
        | Success -> Next (sibling, bb)
        | Failure -> Status Failure
        | Running -> invalidOp "Not complete status"
    fun parentComplete ->
        Array.foldBack
            (fun factory sibling -> factory (onComplete sibling))
            (children.[..children.Length-2])
            (Array.last children <| parentComplete)
            
let Selector (children: _[]) =
    let onComplete sibling status bb =
        match status with
        | Failure -> Next (sibling, bb)
        | Success -> Status Success
        | Running -> invalidOp "Not complete status"
    fun parentComplete ->
        Array.foldBack
            (fun factory sibling -> factory (onComplete sibling))
            (children.[..children.Length-2])
            (Array.last children <| parentComplete)
            

let Parallel (children: _[]) =
    
        
                    
    fun parentComplete ->
        let tick activeChildren data bb =
            let next = activeChildren |>
                        Array.map (Tick data bb) |>
                        Array.filter (fun r -> match r with | Status s -> false | _ -> true)        
            if next.Length = 0 then Status Success
            else Next {Node={Node.Default with Tick = tick next}; OnComplete=parentComplete}
        {Node = {Node<_,_>.Default with Tick = tick children}; OnComplete=parentComplete}
        
            
        
        
            
let Leaf node onComplete = {Node=node; OnComplete=onComplete}
let mutable a = 0
let FailNode: (Status -> Map<int, obj> -> TickResult<int, int>) -> BuiltNode<int, int> =
    Leaf <| {Node<int, int>.Default with Tick = fun _ bb -> Failure, bb}
let SuccessNode: (Status -> Map<int, obj> -> TickResult<int, int>) -> BuiltNode<int, int> =
    Leaf {Node<int, int>.Default with Tick = fun _ bb -> a <- a + 1; Success, bb}
    
let A = Sequence [| SuccessNode; FailNode |]
*)
type Blackboard = int
type Data = int
type NodeX = Data * Blackboard -> TickResult
and TickResult = | End of Status | Next of NodeX
and Tick = Data * Blackboard -> Status
type NodeAction = Data * Blackboard -> TickResult

 
let rec Action (fn: Tick) (init: unit -> unit) =
    let doInit () = init()
    fun (parent: NodeAction) ->
        //(data: Data, blackboard: Blackboard)
        let rec _tick arg =
            match fn arg with
            | Running -> Next <| _tick
            | result -> parent arg
        fun blackboard ->
            doInit()
            _tick
            
        
    
let Parallel (children: _[]) =
    fun (parent: NodeAction) ->
        let rec tick (nodes: NodeX[]) arg =
            let n =
                nodes |>
                Array.map (fun n -> n arg) |>  
                Array.choose (fun r -> match r with | End _ -> None | Next n -> Some(n))
            if n.Length = 0 then parent arg
            else Next <| tick n
        let preparedChildren = Array.map (fun c -> c End) children
        fun blackboard ->
            tick (Array.map (fun c -> c blackboard) preparedChildren)
    
let Selector (children: _[]) =
    let onComplete sibling (status, bb) =
        match status with
        | Failure -> Next (sibling bb)
        | Success -> End Success
        | Running -> invalidOp "Not complete status"
    fun (parentComplete: NodeAction) ->
        let preparedChildren = Array.map (fun c -> c parentComplete) children 
        fun blackboard ->
            Array.foldBack
                (fun factory sibling -> factory (onComplete sibling))
                (preparedChildren.[..children.Length-2])
                (Array.last preparedChildren <| blackboard)
            
let mutable a = 0
let FailNode = Action (fun _ -> Failure) (fun _ -> ())
let SuccessNode = Action (fun _ -> Success) (fun _ -> ())
let A= Selector [| SuccessNode; FailNode |]
[<EntryPoint>]
let main argv =
    let rootComplete = End
    printfn "%A" <| (A End) (0, 0)
    //printfn "%A" <| AllTicks (A rootComplete) 0 Map.empty
    //printfn "%A" a
    0 // return an integer exit code
