module Yggdrasil.BehaviorTree

open FSharpx.Collections

type Status = Success | Failure | Running | Aborted
type SuccessCondition = SuccessOne | SuccessAll
type FailureCondition = FailureOne | FailureAll

type State =
    abstract member Increase: unit -> unit
    abstract member Fail: unit -> unit
type CompleteCallback = Status -> unit

[<AbstractClass>]
type Node(onComplete: OnCompleteCallback) =
    abstract member OnComplete: OnCompleteCallback
    abstract member Update: State -> unit    
    abstract member Status: Status with get, set
    default this.OnComplete = onComplete
    default val Status = Running with get, set
    
and OnCompleteCallback = Status -> Q -> Q
and Q = Queue<Node>
and Factory = OnCompleteCallback -> Node

//let FlagNode = {new Node(BlankBranch) with member this.Update _ = Running}

(*
let UpdateNode (queue: Q) state =
    match queue.TryDequeue() with
    | true, node ->
        if node = FlagNode then Some(Running) 
        else
            let status = node.Update(state)
            match status with
                | Aborted -> queue.Clear()
                | Running -> queue.Enqueue node
                | result -> node.Parent.OnChildCompleted result
            Some(status)
    | false, _ -> None
    
let Step (queue: Q) state =
    queue.Enqueue FlagNode
    let rec update s =
        match queue.TryDequeue() with
        | true, node ->
            if node = FlagNode then Some(Running) 
            else
                if node.Status = Running then node.Update(state) |> ignore                
                match node.Status with
                    | Aborted -> queue.Clear()
                    | Running -> queue.Enqueue node
                    | result -> node.Parent.OnChildCompleted result
                update <| Some(node.Status)
        | false, _ -> s
    update None
*)

let RootComplete _ q = q
let InitTree (tree: Factory) = Queue.empty.Conj <| tree RootComplete

let rec Tick (queue: Q) (nextQueue: Q) state =
    let (node, tail) = queue.Uncons 
    if node.Status = Running then node.Update state
    if node.Status = Aborted then Queue.empty, Aborted            
    else
        let next = match node.Status with
                    | Running -> nextQueue.Conj node
                    | Success | Failure -> node.OnComplete node.Status nextQueue
                    | _ -> invalidArg (string node.Status) "Invalid status for node.Update"
        if tail.IsEmpty then next, if next.IsEmpty then node.Status else Running
        else Tick tail next state 
            
let rec AllTicks (queue: Q) state status =
    if queue.IsEmpty then status
    else
        let (nextQueue, nextStatus) = Tick queue Queue.empty state
        AllTicks nextQueue state nextStatus
         
let Execute (tree: Factory) state =
    let rootComplete _ q = q
    let queue = Queue.empty.Conj <| tree rootComplete
    AllTicks queue state Running
    

let Monitor(condition: Factory, node: Factory) =    
    fun onComplete ->
        let mutable subtree =  InitTree node
        {
            new Node(onComplete) with
                member this.Update state =
                    let conditionOk = Execute condition state = Success
                    if conditionOk then
                        let (next, status) = Tick subtree Queue.empty state
                        subtree <- next
                        this.Status <- status
                    else this.Status <- Aborted
        }
    
    (*
let Run (tree: Factory) state =
    let queue = Queue<Node>()
    tree BlankBranch queue
    let rec allSteps (q: Q) s =
        if q.Count > 0 then allSteps q (Step q state)
        else s
    allSteps queue None
*)

module SequenceNode =
    let rec OnChildCompleted children parentCallback status (queue: Q) =
        match status with
            | Failure -> parentCallback Failure queue
            | Success ->
                match children with
                | [||] -> parentCallback Success queue
                | _ -> queue.Conj <| Build children parentCallback                    
            | _ -> invalidArg "Status" "Invalid termination Status for Sequence"            
    and Build (children: Factory[]) parentCallback =
        let callback = OnChildCompleted children.[1..] parentCallback
        children.[0] callback
        
let Sequence (children: Factory[]) = SequenceNode.Build children

(*        
type MonitorNode(parent, queue: Q, condition: Factory) as this =
    inherit Node(parent)
        
    interface Branch with
        member this.OnChildCompleted status = this.Status <- status
    override this.Update state =
        if this.Status = Running then
            this.Status <- match Run condition state with
                            | Some(result) ->
                                match result with
                                | Failure -> Aborted
                                | Success -> Running
                                | s -> invalidArg (string s) "Invalid result for Condition subtree"
                            | None -> invalidArg "condition" "Invalid Condition for MonitorNode"
        this.Status
        
let Monitor(condition: Factory, node: Factory) =    
    fun parent queue ->
        let monitor = MonitorNode(parent, queue, condition)
        queue.Enqueue monitor
        node monitor queue
        //monitor :> Node
*)
    
let Action (action) =
    fun onComplete ->
        let node = {
            new Node(onComplete) with
            member this.Update state = this.Status <- action state}
        node

(*
[<AbstractClass>]self
type Node(parent: Node option) =    
    abstract member Init: Node option -> Node
    abstract member Update: S -> NodeResult
    abstract member OnChildComplete: Status -> NodeResult
    abstract member Parent: Node option
    abstract member Terminate: unit -> unit
    default this.Terminate () = ()
    default this.Update (_: S): NodeResult = invalidOp "Not implemented for this type"
    default this.OnChildComplete (_: Status): NodeResult = invalidOp "Not implemented for this type"
    default this.Parent = parent
and NodeResult =
    | Status of Status
    | Tree of Node
and Q = Queue<Node>

and S =    
    abstract member Increase: unit -> unit
    
let rec HandleChildResult (queue: Q) (node: Node option) status =
    if status = Running then queue
    else
        match node with
        | Some n ->
            match n.OnChildComplete status with
            | Status result -> HandleChildResult queue n.Parent result
            | Tree t -> queue.Conj t
        | None -> queue

let rec Step (queue: Queue<Node>) (nextQueue: Queue<Node>) state =
    let (node, prevQ) = queue.Uncons
    let resultQueue, cancel = match node.Update state with
                                | Status result ->
                                    HandleChildResult nextQueue node.Parent result, false
                                | Tree t -> (nextQueue.Conj t), true
    if cancel || prevQ.Length = 0 then resultQueue
    else Step prevQ resultQueue state
    
let rec AllSteps queue state =
    match Step queue Queue.empty state with
    | q when q = Queue.empty -> state
    | newQ -> AllSteps newQ state
    
let Run (tree: Node) state =
    let queue = Queue.empty.Conj (tree.Init None)
    AllSteps queue state
    
type Sequence(parent:Node option, children: Node[]) =
    inherit Node(parent)
    override this.OnChildComplete status =
        match status with
        | Failure -> Status Failure
        | Success ->
            match children with
            | [||] -> Status Success
            | _ -> Tree <| (this :> Node).Init parent                   
        | _ -> invalidArg "Status" "Invalid termination Status for Sequence"
    override this.Init parent = children.[0].Init <| Some (Sequence(parent, children.[1..]) :> Node)

    new(children) = Sequence(None, children)

type ActiveSelector(parent: Node option, high: Node, low: Node, switch) =
    inherit Node(parent)
    override this.Init parent = ActiveSelector(parent, high, low, switch) :> Node
    override this.Update state =
        if switch state then Tree <| high.Init parent
        else Tree <| (ActiveSelectorLowPriority(parent, high, low, switch) :> Node)

    new(high, low, switch) = ActiveSelector(None, high, low, switch)
and ActiveSelectorLowPriority(parent: Node option, high: Node, node: Node, switch) =
    inherit Node(parent)
    override this.Init _ = invalidOp "Invalid Op for type"
    override this.Update state =
        if switch state then Tree <| high.Init parent
        else node.Update state
        
type Parallel(parent: Node option, success, failure, nodes: Node[]) =
    inherit Node(parent)
    override this.OnChildComplete status =
        if nodes = [||] then Status status
        else
            match status with
            | Success ->
                if success = SuccessOne then Status Success
                else Tree <| (this :> Node).Init parent
            | Failure ->
                if failure = FailureOne then Status Failure
                else Tree <| (this :> Node).Init parent
            | _ -> invalidArg "" ""
    override this.Init parent =
        let next = Parallel(parent, success, failure, nodes.[1..]) :> Node
        nodes.[0].Init <| Some next
        
type Monitor(parent: Node option, conditions: Node[], node: Node) =
    inherit Node(parent)
    override this.Init parent = MonitorInternal(parent, conditions, node.Init parent) :> Node
    
and MonitorInternal(parent, conditions: Node[], node) =
    inherit Node(parent)
    let ConditionFail state (condition: Node) =
        let result = condition.Update state
        match result with
        | Status s -> s = Failure
        | Tree _ -> invalidArg "Condition" "Condition nodes must return a Status"
    override this.Init parent = MonitorInternal(parent, conditions, node) :> Node
    override this.Update state =
        let result = Array.tryFind (ConditionFail state) conditions
        match result with
        | None -> node.Update state
        | Some(_) -> Status Failure
                        
type Action(parent, action) =
    inherit Node(parent)
    override this.Init parent = Action(parent, action) :> Node
    override this.Update state = Status <| action state
    new(action) = Action(None, action)
*)