module BehaviorTree

open Moq
open NUnit.Framework
open Yggdrasil.Behavior.BehaviorTree

type State =
    abstract member Increase: unit -> unit
    abstract member Fail: unit -> unit
let IncreaseSuccessNode = Action (fun (s: State) -> s.Increase(); Success)
let IncreaseFailureNode = Action (fun (s: State) -> s.Increase(); Failure)
let SuccessNode = Action (fun _ -> Success)
let FailureNode = Action (fun _ -> Failure)
[<Test>]
let ``Sequence Executes All Children`` () =
    let mock = Mock<State>()
    let seq = Sequence([|IncreaseSuccessNode; IncreaseSuccessNode; IncreaseSuccessNode;|])
    let result = Execute seq mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Success, result)
    
[<Test>]
let ``Sequence Exits Early if Child Fails`` () =
    let mock = Mock<State>()
    let seq = Sequence([|IncreaseSuccessNode; FailureNode; IncreaseSuccessNode;|])
    let result = Execute seq mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Failure, result)
    
[<Test>]
let ``Selector Executes All Children`` () =
    let mock = Mock<State>()
    let sel = Selector([|IncreaseFailureNode; IncreaseFailureNode; IncreaseFailureNode;|])
    let result = Execute sel mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Failure, result)
    
[<Test>]
let ``Selector Exits Early if Child Succeeds`` () =
    let mock = Mock<State>()
    let sel = Selector([| IncreaseSuccessNode; IncreaseFailureNode;|])
    let result = Execute sel mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Success, result)
    
[<Test>]
let ``Parallel returns first child result if OneSuccess OneFail``() =
    let mock = Mock<State>()
    let pal = Parallel([|FailureNode; IncreaseSuccessNode|], ParallelFlag.OneSuccess ||| ParallelFlag.OneFailure)
    let result = Execute pal mock.Object    
    Assert.AreEqual(Failure, result)
    
    let pal2 = Parallel([|SuccessNode; IncreaseFailureNode|], ParallelFlag.OneSuccess ||| ParallelFlag.OneFailure)
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Success, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Never)
    
[<Test>]
let ``Parallel returns first Success or result of last child if OneSuccess AllFail``() =
    let mock = Mock<State>()
    let pal = Parallel([|FailureNode; IncreaseSuccessNode|], ParallelFlag.OneSuccess)
    let result = Execute pal mock.Object    
    Assert.AreEqual(Success, result)
    
    let pal2 = Parallel([|FailureNode; IncreaseFailureNode|], ParallelFlag.OneSuccess)
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Failure, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))
    
[<Test>]
let ``Parallel returns first Failure or result of last child if AllSuccess OneFail``() =
    let mock = Mock<State>()
    let pal = Parallel([|FailureNode; IncreaseSuccessNode|], ParallelFlag.OneFailure)
    let result = Execute pal mock.Object    
    Assert.AreEqual(Failure, result)
    
    let pal2 = Parallel([|SuccessNode; IncreaseSuccessNode|], ParallelFlag.OneFailure)
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Success, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    
[<Test>]
let ``Parallel returns last child if AllSuccess AllFail``() =
    let mock = Mock<State>()
    let pal = Parallel([|FailureNode; IncreaseSuccessNode|], ParallelFlag.AllFailure ||| ParallelFlag.AllSuccess)
    let result = Execute pal mock.Object    
    Assert.AreEqual(Success, result)
    
    let pal2 = Parallel([|SuccessNode; IncreaseFailureNode|], ParallelFlag.AllFailure ||| ParallelFlag.AllSuccess)
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Failure, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))