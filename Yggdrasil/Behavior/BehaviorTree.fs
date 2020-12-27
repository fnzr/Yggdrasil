module Yggdrasil.Behavior.BehaviorTree

open FSharpx.Collections

type Status = Success | Failure | Running | Aborted
type SuccessCondition = SuccessOne | SuccessAll
type FailureCondition = FailureOne | FailureAll

type Node<'T> = {
    OnComplete: OnCompleteCallback<'T>
    OnTick: 'T -> Status
}
and OnCompleteCallback<'T> = Status -> Queue<Node<'T>> -> Queue<Node<'T>>
type Factory<'T> = OnCompleteCallback<'T> -> Node<'T>

let RootComplete _ q = q
let InitTree (tree: Factory<'T>) = Queue.empty.Conj <| tree RootComplete

let rec _Tick (queue: Queue<Node<'T>>) (nextQueue: Queue<Node<'T>>) state =
    let (node, tail) = queue.Uncons
    let result = node.OnTick state
    let next = match result with
                | Aborted -> Queue.empty
                | Running -> nextQueue.Conj node
                | Success | Failure ->  node.OnComplete result nextQueue
    if tail.IsEmpty then next, if next.IsEmpty then result else Running
    else _Tick tail next state    

let Tick (queue: Queue<Node<'T>>) state = _Tick queue Queue.empty state
            
let rec AllTicks (queue: Queue<Node<'T>>) state status =
    if queue.IsEmpty then status
    else
        let (nextQueue, nextStatus) = _Tick queue Queue.empty state
        AllTicks nextQueue state nextStatus
         
let Execute tree state =
    let rootComplete _ q = q
    let queue = Queue.empty.Conj <| tree rootComplete
    AllTicks queue state Running
    
let Monitor condition node =    
    fun onComplete ->
        let mutable subtree =  InitTree node
        {
            OnComplete = onComplete
            OnTick = fun state ->
                if Execute condition state = Success then
                    let (next, status) = _Tick subtree Queue.empty state
                    subtree <- next; status
                else Aborted
        }
        
let Loop node =
    fun onComplete ->
        let mutable subtree = InitTree node
        {
            OnComplete = onComplete
            OnTick = fun state ->
                let (next, status) = _Tick subtree Queue.empty state
                subtree <- if status = Running then next else InitTree node
                Running
        }


let Action (action) =
    fun onComplete ->
        let node = {        
            OnComplete = onComplete
            OnTick = action
        }
        node
    
module SequenceNode =
    let rec OnChildCompleted children parentCallback status (queue: Queue<Node<'T>>) =
        match status with
            | Failure -> parentCallback Failure queue
            | Success ->
                match children with
                | [||] -> parentCallback Success queue
                | _ -> queue.Conj <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Sequence"            
    and Build (children: Factory<'T>[]) parentCallback =
        let sequenceChildComplete = OnChildCompleted children.[1..] parentCallback
        children.[0] sequenceChildComplete
        
let Sequence (children: Factory<'T>[]) = SequenceNode.Build children

module SelectorNode =
    let rec OnChildCompleted children parentCallback status (queue: Queue<Node<'T>>) =
        match status with
            | Success -> parentCallback Success queue
            | Failure ->
                match children with
                | [||] -> parentCallback Failure queue
                | _ -> queue.Conj <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Selector"            
    and Build (children: Factory<'T>[]) parentCallback =
        let selectorChildComplete = OnChildCompleted children.[1..] parentCallback
        children.[0] selectorChildComplete
        
let Selector (children: Factory<'T>[]) = SelectorNode.Build children