// Learn more about F# at http://fsharp.org

open System
open Moq
open Yggdrasil.BehaviorTree

let a (v: State) = v.Increase(); Success

type ConditionNode(onComplete) =
    inherit Node(onComplete)
    let responses = [|Success; Success; Success; Failure|]
    static member val i = 0 with get, set
                
    override this.Update (_: State) =
        this.Status <- responses.[ConditionNode.i]
        ConditionNode.i <- ConditionNode.i + 1
        ()//this.Status |> ignore
    
let ConditionFactory =
    fun (onComplete: OnCompleteCallback) -> ConditionNode(onComplete) :> Node
[<EntryPoint>]
let main argv =    
    let mock = Mock<State>()
    
    let a = Sequence([|Action(a); Action(a); Action(a); Action(a)|])
    let tree = Monitor(ConditionFactory, a)
                       
    
    printfn "%A" <| Execute tree mock.Object 
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))
    //printfn "%A" mock.Object
    0 // return an integer exit code
