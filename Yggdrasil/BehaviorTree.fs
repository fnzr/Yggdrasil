module Yggdrasil.BehaviorTree

type Status = Success | Failure | Initializing | Running

type ActionResult<'T> =
    |Action of Action<'T>
    |Result of Status
and Action<'T> = 'T -> ActionResult<'T>

type Node<'T> =
    | Branch of (Node<'T> list -> Status -> Action<'T>)
    | Leaf of (Node<'T> list -> Action<'T>)
    
let Move<'T> (node: Node<'T>) stack status =
    match node with
    | Branch b -> b stack status
    | Leaf f -> f stack

let rec Sequence (children: Node<'T>[]) (stack: Node<'T> list) status =
    match status with
    | Initializing ->
        let s = (Branch <| Sequence children.[1..]) :: stack
        Move children.[0] s Initializing
    | Failure -> Move stack.Head stack.Tail Failure
    | Success ->
        match children with
        | [||] -> Move stack.Head stack.Tail Success                  
        | remaining ->
            let s = (Branch <| Sequence remaining.[1..]) :: stack
            Move remaining.[0] s Initializing
    | Running -> invalidOp "Invalid status for Sequence"
    
let rec Selector (children: Node<'T>[]) (stack: Node<'T> list) status =
    match status with
    | Initializing ->
        let s = (Branch <| Selector children.[1..]) :: stack
        Move children.[0] s Initializing
    | Success -> Move stack.Head stack.Tail Success
    | Failure ->
        match children with
        | [||] -> Move stack.Head stack.Tail Success                  
        | remaining ->
            let s = (Branch <| Selector remaining.[1..]) :: stack
            Move remaining.[0] s Initializing
    | Running -> invalidOp "Invalid status for Selector"

let rec Action<'T> (action: 'T -> Status) (stack: Node<'T> list) state =
    let result = action state
    match result with
    | Running -> ActionResult.Action (Action action stack)
    | status -> Move stack.Head stack.Tail status state
    