module Yggdrasil.BehaviorTree

open FSharpx.Collections

type Status = Success | Failure | Running
type SuccessCondition = SuccessOne | SuccessAll
type FailureCondition = FailureOne | FailureAll

(*
module Nodes =
    type Node<'T> =
        abstract member Cancel: unit -> unit
        abstract member Terminate: Status -> unit
        abstract member Update: 'T -> Status
        //abstract member Start: Branch<'T> option -> BehaviorQueue<'T> -> unit
    and Branch<'T> =
        abstract member OnChildComplete: Status -> unit    
    and BehaviorQueue<'T> = Queue<Node<'T>>
    and NodeFactory<'T> = Branch<'T> option -> BehaviorQueue<'T> -> unit
    
    let ReportToParent (parent: Branch<'T> option) status =
        match parent with
        | Some(p) -> p.OnChildComplete status
        | None -> ()
        
    type SequenceNode<'T>(parent: Branch<'T> option, queue: BehaviorQueue<'T>, children: NodeFactory<'T>[]) =
        interface Branch<'T> with
            member this.OnChildComplete status =
                match status with
                | Failure -> (this :> Node<'T>).Terminate Failure
                | Success ->
                    match children with
                    | [||] -> (this :> Node<'T>).Terminate Success
                    | _ -> SequenceNode.Setup parent queue children
                | _ -> invalidArg "Status" "Invalid termination Status for Sequence"
                
        interface Node<'T> with
            member this.Cancel () = ()
            member this.Update _ = invalidOp "Update is not defined for Sequence"
            member this.Terminate status = ReportToParent parent status
            
        static member Setup parent queue (children: NodeFactory<'T>[]) =
            let seq = Some(SequenceNode(parent, queue, children.[1..]) :> Branch<'T>)
            children.[0] seq queue
                
    type Action<'T>(action, parent: Branch<'T> option) =
        interface Node<'T> with
            member this.Terminate status = ReportToParent parent status
            member this.Cancel () = ()
            member this.Update s = action s
            
    type ActiveSelector<'T>(switch, parent: Branch<'T> option, queue: BehaviorQueue<'T>, high: NodeFactory<'T>, low: NodeFactory<'T>) =
        interface Node<'T> with
            member this.Cancel () = ()
            member this.Update state =
                if switch state then
                    high parent queue
                    Success
                else
                    //queue.Enqueue <| (this :> Node<'T>)
                    low parent queue
                    Running
            member this.Terminate status = ReportToParent parent status
            
        static member Setup switch parent (queue: BehaviorQueue<'T>) high low =
            let sel = ActiveSelector(switch, parent, queue, high, low) :> Node<'T>
            queue.Enqueue sel
            
                    
let Sequence (children: Nodes.NodeFactory<'T>[]) =
    fun parent queue -> Nodes.SequenceNode.Setup parent queue children
let Action<'T> action =
    fun parent (queue: Nodes.BehaviorQueue<'T>) -> queue.Enqueue <| Nodes.Action(action, parent)
*)
// ChildComplete: Queue -> Status -> Queue
// Sequence: Node[] -> Queue -> Callback -> _ -> Queue
// Action: Queue -> Callback -> state -> Queue
// ActiveSelector: Queue -> Callback -> state -> Queue
[<AbstractClass>]
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
