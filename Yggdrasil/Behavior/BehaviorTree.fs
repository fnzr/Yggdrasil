module Yggdrasil.Behavior.BehaviorTree

open NLog

type Status = Success | Failure
type ParallelFlag = OneSuccess | AllSuccess 

and Tick<'data> = 'data -> Result<'data>
and Result<'data> =
    | End of Status
    | Next of Tick<'data>

type Continuation<'data> = Status -> 'data -> Result<'data>
type Node<'data> = Continuation<'data> -> Tick<'data>

let Logger = LogManager.GetLogger "BehaviorTree"

let StatusRoot status _ = End status

let rec NoOpNode _ = Next NoOpNode
let NoOpRoot _ = Next NoOpNode  

let Condition fn parent data =
    if fn data then parent Success data else parent Failure data

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
let Parallel (children: _[]) successCondition parent data =
    let rec tick nodes data =
        let (completed, running) = _ParallelTick nodes data
        let (successCount, failCount) = _CountParallelResult completed
        if running.Length = 0 then
            if successCondition = ParallelFlag.AllSuccess && failCount > 0
                then parent Failure data
            else parent Success data
        else if successCount > 0 && successCondition = ParallelFlag.OneSuccess
            then parent Success data
        else
            Next (tick <| List.toArray running)
    tick (Array.map (fun c -> c StatusRoot data) children)
     
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
        let onResult sibling status data =
            match status with
            | Success -> sibling data
            | Failure -> continuation Failure data        
        let preparedChild = _FoldChildren children onResult continuation
        preparedChild
        
let While condition child  =
    fun continuation ->        
        let rec onResult status data =
            if condition data then Next (child onResult)
            else continuation status data
        child onResult

let Forever child =
    fun _ ->
        let rec onResult _ = Next (child onResult)
        child onResult
        
let UntilSuccess child =
    fun continuation ->
        let rec onResult status data =
            match status with
            | Success -> continuation status data
            | Failure -> Next (child onResult)
        child onResult

let RetryTimeout currentTime (timeout: int64) child parent =
    let rec _retryTimeout initialTime tick data =
        match tick data with
        | Next next -> Next <| _retryTimeout initialTime next
        | End status ->
            if status = Success then parent Success data
            else
                if currentTime() - initialTime > timeout then parent Failure data
                else Next <| _retryTimeout initialTime (child StatusRoot)
    fun data -> _retryTimeout (currentTime()) (child StatusRoot) data
  
let Not child =
    fun continuation ->
        let onResult status data =
            match status with
            | Success -> continuation Failure data
            | Failure -> continuation Success data
        child onResult
