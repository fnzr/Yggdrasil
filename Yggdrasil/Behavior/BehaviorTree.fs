module Yggdrasil.Behavior.BehaviorTree

open System
open FSharpx.Collections
open NLog
type Status = Success | Failure | Running | Invalid
[<Flags>]
type ParallelFlag =
    OneSuccess = 1 | OneFailure = 2 | AllSuccess = 4 | AllFailure = 8

let RootComplete _ _ q = q

[<AbstractClass>]
type Node<'T>(parentName, onComplete) = 
    let mutable _aborted = false
    let mutable _onComplete = onComplete

    abstract member OnComplete: OnCompleteCallback<'T> with get, set
    abstract member Tick: 'T -> Status
    abstract member Aborted: bool with get, set
    abstract member Name: string
    abstract member Abort: unit -> unit
    default this.Aborted
        with get() = _aborted and set v = _aborted <- v
    default this.OnComplete
        with get() = _onComplete and set v = _onComplete <- v
    default this.Abort () = this.Aborted <- true
    member this.FullName = sprintf "%s:%s" parentName this.Name 
and OnCompleteCallback<'T> = Status -> 'T -> Queue<Node<'T>> -> Queue<Node<'T>>
type Factory<'T> = OnCompleteCallback<'T> -> Node<'T>

and TreeBuilder<'T> =
    | Selector of TreeBuilder<'T>[]
    | Sequence of TreeBuilder<'T>[]
    | Parallel of ParallelFlag * TreeBuilder<'T>[]
    | Action of ('T -> Status)
    | Factory of (string -> Factory<'T>)
    | Decorator of string * (Factory<'T> -> Factory<'T>) * TreeBuilder<'T>
    | PartialDecorator of string * (Factory<'T> -> Factory<'T>)
    
let Logger = LogManager.GetLogger("BehaviorTree")
let InitTree (tree: Factory<_>) = Queue.empty.Conj <| tree RootComplete
    
let InitTreeOrEmpty tree =
    match tree with
    | Some t -> InitTree t
    | None -> Queue.empty

let rec SkipAbortedNodes (queue: Queue<Node<_>>) =
    match queue.TryUncons with
    | Some (node, tail) ->
        if node.Aborted then SkipAbortedNodes tail
        else Some queue
    | None -> None
let rec _Tick (queue: Queue<Node<'T>>) (nextQueue: Queue<Node<'T>>) state =     
    let (node, tail) = queue.Uncons
    let result = node.Tick state
    Logger.Trace ("{node}: {result}", node.FullName, result)
    let next = match result with
                | Running -> nextQueue.Conj node
                | Success | Failure -> node.OnComplete result state nextQueue
                | Invalid -> invalidOp "Node in invalid state"
    match SkipAbortedNodes tail with
    | None -> next, if next.IsEmpty then result else Running
    | Some q -> _Tick q next state

let Tick (queue: Queue<_>) state =
    if queue.IsEmpty then queue, Invalid
    else _Tick queue (Queue.empty) state
            
let rec AllTicks (queue: Queue<_>) state status =
    if queue.IsEmpty then status
    else
        let (nextQueue, nextStatus) = _Tick queue (Queue.empty) state
        AllTicks nextQueue state nextStatus
        
let EnqueueAll queue nodes onComplete =
    nodes onComplete |>
    Array.fold (fun (q: Queue<_>) -> q.Conj) queue 
         
let Execute tree state = AllTicks (InitTree tree) state Running

let Loop node =
    fun _ ->
        let rec loopComplete _ (queue: Queue<'T>) = queue.Conj <| node loopComplete
        node loopComplete
        
let rec While condition =
    let rec decorator =
        fun factory onComplete ->
            let rec onNodeComplete status state (queue: Queue<Node<'T>>) =
                if condition state then
                    queue.Conj <| factory onNodeComplete
                else onComplete status state queue
            factory onNodeComplete
    PartialDecorator ("While", decorator)

let GenericAction name parentName action =    
    fun onComplete ->
        { new Node<'T>(parentName, onComplete) with
                member this.Tick data = action data
                member this.Name = name
        }

module SequenceNode =
    let rec OnChildCompleted children parentCallback status state (queue: Queue<Node<'T>>) =
        match status with
            | Failure -> parentCallback Failure state queue
            | Success ->
                match children with
                | [||] -> parentCallback Success state queue
                | _ -> queue.Conj <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Sequence"            
    and Build (children: Factory<'T>[]) parentCallback =
        if children.Length = 0
            then raise <| invalidArg "children" "Sequence must have at least one child"
        let sequenceChildComplete = OnChildCompleted children.[1..] parentCallback
        children.[0] sequenceChildComplete

module SelectorNode =
    let rec OnChildCompleted children parentCallback status state (queue: Queue<Node<'T>>) =
        match status with
            | Success -> parentCallback Success state queue
            | Failure ->
                match children with
                | [||] -> parentCallback Failure state queue
                | _ -> queue.Conj <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Selector"            
    and Build (children: Factory<'T>[]) parentCallback =
        if children.Length = 0
            then raise <| invalidArg "children" "Selector must have at least one child"
        let selectorChildComplete = OnChildCompleted children.[1..] parentCallback
        children.[0] selectorChildComplete
        
module ParallelNode =
    
    type NodeGroup<'T>(parentName, children) =
        inherit Node<'T>(parentName, RootComplete)
        let mutable children: Queue<Node<'T>> = children 
        override this.Tick state =
            let (queue, status) = Tick children state
            children <- queue
            status
        override this.Name = "Parallel"
        override this.Abort () =
            let rec abortQueue (queue: Queue<Node<'T>>) =
                match queue.TryUncons with
                | Some (n, q) -> n.Abort(); abortQueue q
                | None -> ()
            abortQueue children
        
    type Parallel<'T> = {
        Flags: ParallelFlag
        mutable Children: Node<'T>[]
    }
    
    let rec OnChildCompleted (node: Parallel<'T>) parentCallback index status state (queue: Queue<Node<'T>>) =
        let lower = if index > 0 then node.Children.[..index-1] else [||]
        let upper = if index + 1 < node.Children.Length then node.Children.[index+1..] else [||]
        let children = Array.concat [|upper; lower|]
        if children.Length = 0 then parentCallback status state queue
        else
            if (node.Flags.HasFlag(ParallelFlag.OneSuccess) && status = Success) ||
               (node.Flags.HasFlag(ParallelFlag.OneFailure) && status = Failure) then
                Array.iter (fun (c: Node<'T>) -> c.Abort()) children
                parentCallback status state queue
            else                
                let next = { node with Children = children }
                let onComplete = OnChildCompleted next parentCallback
                Array.iteri (fun i (c: Node<'T>) -> c.OnComplete <- onComplete i) children
                queue
            
    let Build parentName conditions (children: Factory<'T>[]) =
        if children.Length = 0
            then raise <| invalidArg "children" "Parallel must have at least one child"
        fun onComplete ->
            let parent: Parallel<'T> = {
                Flags = conditions
                Children = [||]
            }
            let buildCb = fun i f -> f <| OnChildCompleted parent onComplete i
            let builtChildren = Array.mapi buildCb children
            parent.Children <- builtChildren
            NodeGroup(parentName,
                      Array.fold (fun (q: Queue<Node<'T>>) -> q.Conj) Queue.empty builtChildren)
            :> Node<'T>
            
            

let Parallel flag = Parallel (flag, [||]) 
let Sequence = Sequence [||]
let Selector = Selector [||]

let withChild branch node =
    match branch with
    | Selector c -> TreeBuilder.Selector <| Array.append c [| node |]
    | Sequence c -> TreeBuilder.Sequence <| Array.append c [| node |]
    | Parallel (f, c) -> TreeBuilder.Parallel (f, Array.append c [| node |])
    | PartialDecorator (n, f) -> TreeBuilder.Decorator (n, f, node)
    | Decorator (n, _, _) -> invalidArg n "Decorator already has child. Use PartialDecorator as builder"
    | n -> invalidArg (string n) "Node cannot have children" 
let (=>) parent child = withChild parent child 
let BuildTree tree : Factory<'T> =    
    let rec internalBuildTree parentName child node =
        let mapper p i n = internalBuildTree (sprintf "%s:%s" parentName p) i n
        match node with
        | Selector c -> SelectorNode.Build <| Array.mapi (mapper "Selector") c
        | Sequence c -> SequenceNode.Build <| Array.mapi (mapper "Sequence") c
        | Parallel (f, c) -> ParallelNode.Build parentName f <| Array.mapi (mapper "Parallel") c
        | Action a -> GenericAction (sprintf "Child%d" child) parentName a
        | Factory f -> f parentName
        | Decorator (n, d, c) ->
            d <| internalBuildTree (sprintf "%s:%s" parentName n) 0 c
        | PartialDecorator (n, _) -> invalidArg n "Decorator must have exactly one child, but has zero"
    internalBuildTree "Root" 0 tree
