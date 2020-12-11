module Yggdrasil.BehaviorTree

type Status = Success | Failure | Initializing | Running

type INode = interface end

type ActionResult<'T> =
    |Action of ('T -> ActionResult<'T>)
    |Result of Status
and IBranch =
    inherit INode
    abstract Step: stack: INode list -> status: Status -> ('T -> ActionResult<'T>)
and ILeaf<'T> =
    inherit INode
    abstract Step: stack: INode list -> 'T -> ActionResult<'T>
    
let Move (node: INode) (stack: INode list) status =
    match node with
    | :? IBranch as b -> b.Step stack status
    | :? ILeaf<'T> as l -> l.Step stack
    | _ -> invalidOp "Invalid subtype for node"    

type Action<'T>(action: 'T -> Status) =
    interface ILeaf<'T> with
        member this.Step (stack: INode list) state =
            let result = action state
            match result with
            | Running -> ActionResult.Action <| (this :> ILeaf<'T>).Step stack
            | status -> Move stack.Head stack.Tail status state

type Sequence(children: INode[]) =
    interface IBranch with
        member this.Step stack status =
            match status with
            | Initializing ->
                let s = (Sequence children.[1..]) :> INode :: stack
                Move children.[0] s Initializing
            | Failure -> Move stack.Head stack.Tail Failure
            | Success ->
                match children with
                | [||] -> Move stack.Head stack.Tail Success                  
                | remaining ->
                    let s = Sequence(children.[1..]) :> INode :: stack
                    Move remaining.[0] s Initializing
            | Running -> invalidOp "Invalid status for Sequence"
    member this.Step = (this :> IBranch).Step
    