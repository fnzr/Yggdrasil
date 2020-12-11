module Yggdrasil.BehaviorTree

open FSharpx.Collections

type Status = Success | Failure | Initializing | Running

and ActionResult<'T> =
    | Action of Node<'T> * Node<'T> list * Queue<Switch<'T>>
    | Result of Status
and Node<'T> =
    abstract Step: Status -> Node<'T> list -> Queue<Switch<'T>> -> 'T -> ActionResult<'T>
and Switch<'T> = 'T -> (Node<'T> * Node<'T> list) option

let NextStep (node: Node<'T>, stack, switches) state =
    let rec checkSwitches (queue: Queue<Switch<'T>>) =
        match queue.TryUncons with
        | Some(switch, q) ->
            match switch state with
            | Some(newNode, newStack) -> newNode.Step Initializing newStack q state
            | None -> checkSwitches q
        | None -> node.Step Running stack switches state
    checkSwitches switches

type Root<'T>(node: Node<'T>) =
    interface Node<'T> with
        member this.Step status _ _ _ = ActionResult.Result status
    member this.Start state = node.Step Initializing [this :> Node<'T>] Queue.empty state

type Action<'T>(action: 'T -> Status) =
    interface Node<'T> with
        member this.Step status stack queue state =
            match action state with
            | Running -> ActionResult.Action <| (this :> Node<'T>, stack, queue)
            | status -> stack.Head.Step status stack.Tail queue state

type Sequence<'T>(children: Node<'T>[]) =
    interface Node<'T> with
        member this.Step status stack queue state =
            match status with
            | Initializing ->
                let s = Sequence(children.[1..]) :> Node<'T> :: stack
                children.[0].Step Initializing s queue state
            | Failure ->stack.Head.Step Failure stack.Tail queue state
            | Success ->
                match children with
                | [||] -> stack.Head.Step Success stack.Tail queue state
                | remaining ->
                    let s = Sequence(children.[1..]) :> Node<'T> :: stack
                    remaining.[0].Step Initializing s queue state
            | Running -> invalidOp "Invalid status for Sequence"
    member this.Step = (this :> Node<'T>).Step
    
type Selector<'T>(children: Node<'T>[]) =
    interface Node<'T> with
        member this.Step status stack queue state =
            match status with
            | Initializing ->
                let s = Selector(children.[1..]) :> Node<'T> :: stack
                children.[0].Step Initializing s queue state
            | Success ->stack.Head.Step Success stack.Tail queue state
            | Failure ->
                match children with
                | [||] -> stack.Head.Step Failure stack.Tail queue state
                | remaining ->
                    let s = Selector(children.[1..]) :> Node<'T> :: stack
                    remaining.[0].Step Initializing s queue state
            | Running -> invalidOp "Invalid status for Selector"
    member this.Step = (this :> Node<'T>).Step
    
type ActiveSelector<'T>(switch, high: Node<'T>, low: Node<'T>) =
    let ShouldSwitch stack state =
        if switch state then Some(high, stack)
        else None
    
    interface Node<'T> with
        member this.Step status stack queue state =            
            match status with
            | Initializing ->
                if switch state then high.Step status stack queue state
                else
                    let newQ = queue.Conj <| ShouldSwitch stack
                    low.Step status stack newQ state
            | _ -> invalidOp "Invalid status for ActiveSelector"
                    
    