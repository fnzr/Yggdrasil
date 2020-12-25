open Moq
open NUnit.Framework
open Yggdrasil.Behavior.BehaviorTree

type State =
    abstract member Increase: unit -> unit
    abstract member Fail: unit -> unit
let IncreaseSuccessNode (s: State) = s.Increase(); Success
let IncreaseFailureNode (s: State) = s.Increase(); Failure
let SuccessNode (_: State) = Success
let FailureNode (_: State) = Failure
[<Test>]
let ``Sequence Executes All Children`` () =
    let mock = Mock<State>()
    let seq = Sequence([|Action IncreaseSuccessNode;Action IncreaseSuccessNode;Action IncreaseSuccessNode;|])
    let result = Execute seq mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Success, result)
    
[<Test>]
let ``Sequence Exits Early if Child Fails`` () =
    let mock = Mock<State>()
    let seq = Sequence([|Action IncreaseSuccessNode;Action FailureNode;Action IncreaseSuccessNode;|])
    let result = Execute seq mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Failure, result)
    
[<Test>]
let ``Selector Executes All Children`` () =
    let mock = Mock<State>()
    let sel = Selector([|Action IncreaseFailureNode;Action IncreaseFailureNode;Action IncreaseFailureNode;|])
    let result = Execute sel mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Failure, result)
    
[<Test>]
let ``Selector Exits Early if Child Succeeds`` () =
    let mock = Mock<State>()
    let sel = Selector([|Action IncreaseSuccessNode;Action IncreaseFailureNode;|])
    let result = Execute sel mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Success, result)
    
[<Test>]
let ``Monitor Returns Child result if Condition ok`` () =
    let mock = Mock<State>()
    
    let result1 = Execute (Monitor (Action SuccessNode) (Action SuccessNode)) mock.Object
    Assert.AreEqual(Success, result1)
    
    let result2 = Execute (Monitor (Action SuccessNode) (Action FailureNode)) mock.Object
    Assert.AreEqual(Failure, result2)

[<Test>]
let ``Monitor Aborts if Condition Fails`` () =
    let mock = Mock<State>()
    let result = Execute (Monitor (Action FailureNode) (Action IncreaseSuccessNode)) mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Never)
    Assert.AreEqual(Aborted, result)
    
[<EntryPoint>]
let main argv =
    0
    (*
    let mock = Mock<State>()
    let a = Sequence([|Action(a); Action(a); Action(a); Action(a); Action(a)|])
    let tree = Monitor(ConditionFactory, a)
                       
    
    printfn "%A" <| Execute tree mock.Object 
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    //printfn "%A" mock.Object
    0 // return an integer exit code
    *)
