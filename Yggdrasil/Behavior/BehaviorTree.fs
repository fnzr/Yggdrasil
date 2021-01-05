module Yggdrasil.Behavior.BehaviorTree

open System
open Yggdrasil.Types

type Status = Success | Failure | Running | Invalid
[<Flags>]
type ParallelFlag =
    OneSuccess = 1 | OneFailure = 2 | AllSuccess = 4 | AllFailure = 8

type Node<'T> = {
    mutable OnComplete: OnCompleteCallback<'T>
    OnTick: 'T -> Status
    mutable Aborted: bool
}
and Queue<'T> =
    {
        Queue: FSharpx.Collections.Queue<Node<'T>>
    }
    member this.Push (node: Node<'T>) =
        {
            Queue = this.Queue.Conj node
        }
    member this.Pop =
        let (node, queue) = this.Queue.Uncons
        node, {this with Queue = queue}
    member this.TryPop =
        if this.Queue.IsEmpty then None
        else Some this.Pop
    member this.IsEmpty = this.Queue.IsEmpty
    static member empty() = {Queue = FSharpx.Collections.Queue.empty}
and OnCompleteCallback<'T> = Status -> Queue<'T> -> Queue<'T>
type Factory<'T> = OnCompleteCallback<'T> -> Node<'T>[]

let RootComplete _ q = q
let InitTree (tree: Factory<'T>) =
    Array.fold (fun (q: Queue<_>) -> q.Push) (Queue<_>.empty()) <| tree RootComplete
    
let InitTreeOrEmpty tree =
    match tree with
    | Some t -> InitTree t
    | None -> Queue<_>.empty()

let rec SkipAbortedNodes (queue: Queue<'T>) =
    match queue.TryPop with
    | Some (node, tail) ->
        if node.Aborted then SkipAbortedNodes tail
        else Some queue
    | None -> None
let rec _Tick (queue: Queue<'T>) (nextQueue: Queue<'T>) (state: 'T) =     
    let (node, tail) = queue.Pop
    let result = node.OnTick state
    let next = match result with
                | Running -> nextQueue.Push node
                | Success | Failure ->  node.OnComplete result nextQueue
                | Invalid -> invalidOp "Node in invalid state"
    match SkipAbortedNodes tail with
    | None -> next, if next.IsEmpty then result else Running
    | Some q -> _Tick q next state

let Tick (queue: Queue<_>) state =
    if queue.IsEmpty then queue, Invalid
    else _Tick queue (Queue<_>.empty()) state
            
let rec AllTicks (queue: Queue<_>) state status =
    if queue.IsEmpty then status
    else
        let (nextQueue, nextStatus) = _Tick queue (Queue<_>.empty()) state
        AllTicks nextQueue state nextStatus
         
let Execute tree state =
    let rootComplete _ q = q
    let queue = Array.fold (fun (q: Queue<_>) -> q.Push) (Queue<_>.empty()) <| tree rootComplete
    AllTicks queue state Running

let Loop node =
    fun _ ->
        let rec loopComplete _ (queue: Queue<'T>) = queue.Push <| node loopComplete
        node loopComplete

let Action action =
    fun onComplete ->
        [|{
            OnComplete = onComplete
            OnTick = action
            Aborted = false
        }|]

module SequenceNode =
    let rec OnChildCompleted children parentCallback status (queue: Queue<'T>) =
        match status with
            | Failure -> parentCallback Failure queue
            | Success ->
                match children with
                | [||] -> parentCallback Success queue
                | _ -> Array.fold (fun (q: Queue<'T>) -> q.Push) queue <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Sequence"            
    and Build (children: Factory<'T>[]) parentCallback =
        let sequenceChildComplete = OnChildCompleted children.[1..] parentCallback
        children.[0] sequenceChildComplete
        
let Sequence (children: Factory<'T>[]) = SequenceNode.Build children

module SelectorNode =
    let rec OnChildCompleted children parentCallback status (queue: Queue<'T>) =
        match status with
            | Success -> parentCallback Success queue
            | Failure ->
                match children with
                | [||] -> parentCallback Failure queue
                | _ -> Array.fold (fun (q: Queue<'T>) -> q.Push) queue <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Selector"            
    and Build (children: Factory<'T>[]) parentCallback =
        let selectorChildComplete = OnChildCompleted children.[1..] parentCallback
        children.[0] selectorChildComplete
        
let Selector (children: Factory<'T>[]) = SelectorNode.Build children

module ParallelNode =
    
    type Parallel<'T> = {
        Flags: ParallelFlag
        mutable Children: Node<'T>[][]
    }
    
    let rec OnChildCompleted (node: Parallel<'T>) parentCallback index status (queue: Queue<'T>) =
        let lower = if index > 0 then node.Children.[..index-1] else [||]
        let upper = if index + 1 < node.Children.Length then node.Children.[index+1..] else [||]
        let children = Array.concat [|upper; lower|]
        if children.Length = 0 then parentCallback status queue
        else
            let flattened = (children |> Array.reduce Array.append)
            if (node.Flags.HasFlag(ParallelFlag.OneSuccess) && status = Success) ||
               (node.Flags.HasFlag(ParallelFlag.OneFailure) && status = Failure) then
                Array.iter (fun c -> c.Aborted <- true) flattened
                parentCallback status queue
            else                
                let next = { node with Children = children }
                let onComplete = OnChildCompleted next parentCallback
                Array.iteri (fun i c -> c.OnComplete <- onComplete i) flattened
                queue
            
let Parallel (children: Factory<'T>[], conditions) =
    fun onComplete ->
        let parent: ParallelNode.Parallel<'T> = {
            Flags = conditions
            Children = [||]
        }
        let buildCb = fun i f -> f <| ParallelNode.OnChildCompleted parent onComplete i
        let builtChildren = Array.mapi buildCb children
        parent.Children <- builtChildren
        builtChildren |> Array.reduce Array.append