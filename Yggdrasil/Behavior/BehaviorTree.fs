module Yggdrasil.Behavior.BehaviorTree

open NLog

type Status = Success | Failure
type ParallelFlag = OneSuccess | AllSuccess 

type ActiveNode<'data> = 'data -> NodeResult<'data>
and NodeResult<'data> =
    | End of Status
    | Next of ActiveNode<'data>

type ParentContinuation<'data> = 'data * Status -> NodeResult<'data>
type NodeCreator<'data> = ParentContinuation<'data> -> string -> ActiveNode<'data>

let Logger = LogManager.GetLogger "BehaviorTree"

type TickResult<'data, 'store> = Node of Node<'data, 'store> | Result of Status
and Node<'data, 'store> =
    {
        State: 'store
        Initialize: Node<'data, 'store> -> Node<'data, 'store>
        Tick: 'data -> Node<'data, 'store> -> TickResult<'data, 'store>
    }
    static member Stateless tick = {
        State = ()
        Initialize = id
        Tick = tick
    }

let rec NoOp _ = Next NoOpNode
and NoOpNode _ = Next NoOpNode  
let DefaultRoot  (_, status) = End status
let Action (node: Node<_, _>) =
    fun parent ->
        let rec tick (currentNode: Node<_, _>) data =
            //Logger.Debug ("{node:A}", currentNode)
            match node.Tick data currentNode with
            | Result result -> parent (data, result)
            | Node next -> Next <| tick next
        tick (node.Initialize node)
        
let Stateless tick =
    Action {
        State = ()
        Initialize = id
        Tick = tick
    }

let _ParallelTick (children: _[]) data =
    let folder (completed, running) node =
        match node data with
        | End status -> status :: completed, running
        | Next (next) -> completed, next :: running
    Array.fold folder ([], []) children
    
let _CountParallelResult results =
    List.fold
        (fun (sc, fc) r ->
            match r with
            | Success -> (sc+1, fc)
            | Failure -> (sc, fc+1)
        ) (0, 0) results
let Parallel (children: _[]) successCondition =
    fun continuation ->
        let rec tick nodes data =
            let (completed, running) = _ParallelTick nodes data
            let (successCount, failCount) = _CountParallelResult completed
            if running.Length = 0 then
                if successCondition = ParallelFlag.AllSuccess && failCount > 0
                    then continuation (data, Failure)
                else continuation (data, Success)
            else if successCount > 0 && successCondition = ParallelFlag.OneSuccess
                then continuation (data, Success)
            else
                Next (tick <| List.toArray running)
        tick (Array.map (fun c -> c (fun (_, _, s) -> End s)) children)
            
let _FoldChildren (children: _[]) onResult continuation =
    Array.foldBack
        (fun factory sibling -> factory (onResult sibling))
        (children.[..children.Length-2])
        (Array.last children continuation)

let Selector (children: _[]) =    
    fun continuation ->
        let onResult sibling (data, status) =
            match status with
            | Failure -> Next sibling
            | Success -> continuation (data, Success)
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
let Sequence (children: _[]) =    
    fun continuation ->
        let onResult sibling (data, status) =
            match status with
            | Success -> Next sibling
            | Failure -> continuation (data, Failure)        
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
let While condition child  =
    fun continuation ->        
        let rec onResult (data, status) =
            if condition data then Next (child onResult)
            else continuation (data, status)
        child onResult

