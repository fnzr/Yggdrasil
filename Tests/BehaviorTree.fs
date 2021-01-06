module BehaviorTree

open Moq
open NUnit.Framework
open Yggdrasil.Behavior.BehaviorTree

type State =
    abstract member Increase: unit -> unit
    abstract member Fail: unit -> unit
let IncreaseSuccessNode = GenericAction "" "" (fun (s: State) -> s.Increase(); Success)
let IncreaseFailureNode = GenericAction "" "" (fun (s: State) -> s.Increase(); Failure)
let SuccessNode = GenericAction "" "" (fun _ -> Success)
let FailureNode = GenericAction "" "" (fun _ -> Failure)
[<Test>]
let ``Sequence Executes All Children`` () =
    let mock = Mock<State>()
    let seq = SequenceNode.Build ([|IncreaseSuccessNode; IncreaseSuccessNode; IncreaseSuccessNode;|])
    let result = Execute seq mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Success, result)
    
[<Test>]
let ``Sequence Exits Early if Child Fails`` () =
    let mock = Mock<State>()
    let seq = SequenceNode.Build ([|IncreaseSuccessNode; FailureNode; IncreaseSuccessNode;|])
    let result = Execute seq mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Failure, result)
    
[<Test>]
let ``Selector Executes All Children`` () =
    let mock = Mock<State>()
    let sel = SelectorNode.Build ([|IncreaseFailureNode; IncreaseFailureNode; IncreaseFailureNode;|])
    let result = Execute sel mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Failure, result)
    
[<Test>]
let ``Selector Exits Early if Child Succeeds`` () =
    let mock = Mock<State>()
    let sel = SelectorNode.Build ([| IncreaseSuccessNode; IncreaseFailureNode;|])
    let result = Execute sel mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Success, result)
    
[<Test>]
let ``Parallel returns first child result if OneSuccess OneFail``() =
    let mock = Mock<State>()
    let pal = ParallelNode.Build (ParallelFlag.OneSuccess ||| ParallelFlag.OneFailure) [|FailureNode; IncreaseSuccessNode|]
    let result = Execute pal mock.Object    
    Assert.AreEqual(Failure, result)
    
    let pal2 = ParallelNode.Build (ParallelFlag.OneSuccess ||| ParallelFlag.OneFailure) [|SuccessNode; IncreaseFailureNode|]
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Success, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Never)
    
[<Test>]
let ``Parallel returns first Success or result of last child if OneSuccess AllFail``() =
    let mock = Mock<State>()
    let pal = ParallelNode.Build ParallelFlag.OneSuccess [|FailureNode; IncreaseSuccessNode|]
    let result = Execute pal mock.Object    
    Assert.AreEqual(Success, result)
    
    let pal2 = ParallelNode.Build ParallelFlag.OneSuccess [|FailureNode; IncreaseFailureNode|]
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Failure, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))
    
[<Test>]
let ``Parallel returns first Failure or result of last child if AllSuccess OneFail``() =
    let mock = Mock<State>()
    let pal = ParallelNode.Build ParallelFlag.OneFailure [|FailureNode; IncreaseSuccessNode|]
    let result = Execute pal mock.Object    
    Assert.AreEqual(Failure, result)
    
    let pal2 = ParallelNode.Build ParallelFlag.OneFailure [|SuccessNode; IncreaseSuccessNode|]
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Success, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    
[<Test>]
let ``Parallel returns last child if AllSuccess AllFail``() =
    let mock = Mock<State>()
    let pal = ParallelNode.Build (ParallelFlag.AllFailure ||| ParallelFlag.AllSuccess) [|FailureNode; IncreaseSuccessNode|]
    let result = Execute pal mock.Object    
    Assert.AreEqual(Success, result)
    
    let pal2 = ParallelNode.Build (ParallelFlag.AllFailure ||| ParallelFlag.AllSuccess) [|SuccessNode; IncreaseFailureNode|]
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Failure, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))