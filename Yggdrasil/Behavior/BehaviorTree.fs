module Yggdrasil.Behavior.BehaviorTree

type Status = Success | Failure
type ParallelFlag = OneSuccess | AllSuccess 

type ActiveNode<'data, 'blackboard> = 'data -> 'blackboard -> NodeResult<'data, 'blackboard>
and NodeResult<'data, 'blackboard> =
    | End of Status
    | Next of ActiveNode<'data, 'blackboard> * 'blackboard

type TickResult<'blackboard> = Running of 'blackboard | Result of Status * 'blackboard
type ParentContinuation<'data, 'blackboard> = 'data * 'blackboard * Status -> NodeResult<'data, 'blackboard>
type NodeCreator<'data, 'blackboard> = ParentContinuation<'data, 'blackboard> -> 'blackboard -> ActiveNode<'data, 'blackboard> * 'blackboard

type Node<'data, 'blackboard> =
    {
        Tick: 'data -> 'blackboard -> TickResult<'blackboard>
        Initialize: 'blackboard -> 'blackboard
        Finalize: 'blackboard -> 'blackboard
    }
    static member Create tick =
        {
            Tick = tick
            Initialize = id
            Finalize = id
        }
let Action<'a, 'b> (node: Node<'a, 'b>) =
    fun parent ->
        let rec tick data blackboard =
            match node.Tick data blackboard with
            | Result (result, bb) -> parent (data, bb, result)
            | Running bb -> Next (tick, node.Finalize bb)
        fun blackboard -> (tick, node.Initialize blackboard)

let _ParallelTick<'a, 'b> (children: ActiveNode<'a, 'b>[]) data blackboard =
    let folder (bbAcc, completed, running) node =
        match node data bbAcc with
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
let RootComplete<'a, 'b> (_:'a, _:'b, s): NodeResult<'a, 'b> = End s
            
let Parallel<'a, 'b> (children: NodeCreator<'a, 'b>[]) successCondition =
    fun continuation ->
        let rec tick nodes data blackboard =
            let (bb, completed, running) = _ParallelTick nodes data blackboard
            let (successCount, failCount) = _CountParallelResult completed
            if running.Length = 0 then
                if successCondition = ParallelFlag.AllSuccess && failCount > 0
                    then continuation (data, bb, Failure)
                else continuation (data, bb, Success)
            else if successCount > 0 && successCondition = ParallelFlag.OneSuccess
                then continuation (data, bb, Success)
            else
                Next ((tick <| List.toArray running), bb)
        let preparedChildren = Array.map (fun c -> c (fun (_, _, s) -> End s)) children
        fun blackboard ->
            let (ns, bb) =
                Array.fold
                    (fun (nodes, bb) node -> 
                            let (n, bbAccumulator) = node bb
                            n :: nodes, bbAccumulator
                        ) ([], blackboard) preparedChildren
            (tick <| List.toArray ns), bb
            
let _FoldChildren (children: NodeCreator<'a, 'b>[]) onResult continuation =
    Array.foldBack
        (fun factory sibling -> factory (onResult sibling))
        (children.[..children.Length-2])
        (Array.last children continuation)

let Selector<'a, 'b> (children: NodeCreator<'a, 'b>[]) =    
    fun continuation ->
        let onResult sibling (data, bb, status) =
            match status with
            | Failure -> Next (sibling bb)
            | Success -> continuation (data, bb, Success)
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
let Sequence<'a, 'b> (children: NodeCreator<'a, 'b>[]) =    
    fun continuation ->
        let onResult sibling (data, bb, status) =
            match status with
            | Success -> Next (sibling bb)
            | Failure -> continuation (data, bb, Failure)        
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
        
let DefaultRoot<'a, 'b>  (_:'a, _:'b, status): NodeResult<'a, 'b> = End status
        
let While<'a, 'b> (condition: 'a -> bool) (child: NodeCreator<'a, 'b>)  =
    fun continuation ->        
        let rec onResult (data, bb, status) =
            if condition data then Next (child onResult bb)
            else continuation (data, bb, status)
        child onResult

