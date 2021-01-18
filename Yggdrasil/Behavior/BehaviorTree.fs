module Yggdrasil.Behavior.BehaviorTree

type Status = Success | Failure
type ParallelFlag = OneSuccess | AllSuccess 

type ActiveNode<'Data, 'Blackboard> = 'Data * 'Blackboard -> NodeResult<'Data, 'Blackboard>
and NodeResult<'Data, 'Blackboard> = | End of Status | Next of ActiveNode<'Data, 'Blackboard> * 'Blackboard

type TickResult<'Blackboard> = Running of 'Blackboard | Result of Status * 'Blackboard
type ParentContinuation<'Data, 'Blackboard> = 'Data * 'Blackboard * Status -> NodeResult<'Data, 'Blackboard>

type Node<'Data, 'Blackboard> =
    {
        Tick: 'Data * 'Blackboard -> TickResult<'Blackboard>
        Initialize: 'Blackboard -> 'Blackboard
        Finalize: 'Blackboard -> 'Blackboard
    }
    static member Create tick =
        {
            Tick = tick
            Initialize = id
            Finalize = id
        }
let Action<'Data, 'Blackboard> (node: Node<'Data, 'Blackboard>) =
    fun parent ->
        let rec tick (data, blackboard) =
            match node.Tick (data, blackboard) with
            | Result (result, bb) -> parent (data, bb, result)
            | Running bb -> Next (tick, node.Finalize bb)
        fun blackboard -> (tick, node.Initialize blackboard)

let _ParallelTick (children: _[]) (data, blackboard) =
    let folder (bbAcc, completed, running) node =
        match node (data, bbAcc) with
        | End status -> bbAcc, status :: completed, running
        | Next (next, bb2) -> bb2, completed, next :: running
    Array.fold folder (blackboard, [], []) children
    
let _CountParallelResult results =
    List.fold
        (fun (sc, fc) r ->
            match r with
            | Success -> (sc+1, fc)
            | Failure -> (sc, fc+1)
        ) (0, 0) results
let RootComplete<'Data, 'Blackboard> (_: 'Data, _: 'Blackboard, s) = End s
            
let Parallel<'Data, 'Blackboard> (children: _[]) successCondition =
    fun (continuation: ParentContinuation<'Data, 'Blackboard>) ->
        let rec tick nodes (data, blackboard) =
            let (bb, completed, running) = _ParallelTick nodes (data, blackboard)
            let (successCount, failCount) = _CountParallelResult completed
            if running.Length = 0 then
                if successCondition = ParallelFlag.AllSuccess && failCount > 0
                    then continuation (data, bb, Failure)
                else continuation (data, bb, Success)
            else if successCount > 0 && successCondition = ParallelFlag.OneSuccess
                then continuation (data, bb, Success)
            else
                Next ((tick <| List.toArray running), bb)
        let preparedChildren = Array.map (fun c -> c RootComplete) children
        fun blackboard ->
            let (ns, bb) =
                Array.fold
                    (fun (nodes, bb) node -> 
                            let (n: ActiveNode<'Data, 'Blackboard>, bbAccumulator) = node bb
                            n :: nodes, bbAccumulator
                        ) ([], blackboard) preparedChildren
            (tick <| List.toArray ns), bb
            
let _FoldChildren (children: _[]) onResult continuation =
    Array.foldBack
        (fun factory sibling -> factory (onResult sibling))
        (children.[..children.Length-2])
        (Array.last children <| continuation)

let Selector<'Data, 'Blackboard> (children: _[]) =    
    fun continuation ->
        let onResult sibling (data: 'Data, bb: 'Blackboard, status) =
            match status with
            | Failure -> Next (sibling bb)
            | Success -> continuation (data, bb, Success)
        let preparedChild = _FoldChildren children onResult continuation
        fun blackboard -> preparedChild blackboard
        
let Sequence<'Data, 'Blackboard> (children: _[]) =    
    fun (continuation: ParentContinuation<'Data, 'Blackboard>) ->
        let onResult sibling (data: 'Data, bb: 'Blackboard, status) =
            match status with
            | Success -> Next (sibling bb)
            | Failure -> continuation (data, bb, Failure)
        let preparedChild = _FoldChildren children onResult continuation
        fun blackboard -> preparedChild blackboard

let While<'Data, 'Blackboard> (condition: 'Data -> bool) child  =
    fun (continuation: ParentContinuation<_,_>) ->        
        let rec onResult (data, bb, status) =
            if condition data then Next (child onResult bb)
            else continuation (data, bb, status)
        //let builtChild = onResult child
        fun (blackboard: 'Blackboard) -> child onResult blackboard

