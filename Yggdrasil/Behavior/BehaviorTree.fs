module Yggdrasil.Behavior.BehaviorTree

open NLog

type Status = Success | Failure
type ParallelFlag = OneSuccess | AllSuccess 

type ActiveNode<'data> = 'data -> NodeResult<'data>
and NodeResult<'data> =
    | End of Status
    | Next of ActiveNode<'data>

type ParentContinuation<'data> = 'data * Status -> NodeResult<'data>
type NodeCreator<'data> = ParentContinuation<'data> -> ActiveNode<'data>

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
        fun data -> tick (node.Initialize node) <| data
        
let Stateless tick =
    Action {
        State = ()
        Initialize = id
        Tick = tick
    }
    
let Condition fn =
    Stateless <|
    fun data _ -> if fn data then Result Success else Result Failure

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
            | Failure -> sibling data
            | Success -> continuation (data, Success)
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
let Sequence (children: _[]) =    
    fun continuation ->
        let onResult sibling (data, status) =
            match status with
            | Success -> sibling data
            | Failure -> continuation (data, Failure)        
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
let While condition child  =
    fun continuation ->        
        let rec onResult (data, status) =
            if condition data then Next (child onResult)
            else continuation (data, status)
        child onResult

let Forever child =
    fun _ ->
        let rec onResult _ = Next (child onResult)
        child onResult
        
let UntilSuccess child =
    fun continuation ->
        let rec onResult (data, status) =
            match status with
            | Success -> continuation (data, status)
            | Failure -> Next (child onResult)
        child onResult
        
let RetryTimeout currentTime timeout child =
    Action
        {
            State = NoOpNode, 0L
            Initialize = fun instance -> {instance with State = (child DefaultRoot), currentTime()}
            Tick = fun data instance ->                
                match fst instance.State data with
                | Next node -> Node {instance with State=node, snd instance.State}
                | End status ->
                    if status = Success then Result Success
                    else if currentTime() - snd instance.State > timeout then Result Failure
                    else Node {instance with State = (child DefaultRoot), snd instance.State}
        }
    
let Not child =
    fun continuation ->
        let onResult (data, status) =
            match status with
            | Success -> continuation (data, Failure)
            | Failure -> continuation (data, Success)
        child onResult
        
            
