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
    let seq = (Sequence
                   => IncreaseSuccessNode
                   => IncreaseSuccessNode
                   => IncreaseSuccessNode) |> BuildTree
    let result = Execute seq mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Success, result)
    
[<Test>]
let ``Sequence Exits Early if Child Fails`` () =
    let mock = Mock<State>()
    let seq = (Sequence
                   => IncreaseSuccessNode
                   => FailureNode
                   => IncreaseSuccessNode) |> BuildTree
    let result = Execute seq mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Failure, result)
    
[<Test>]
let ``Selector Executes All Children`` () =
    let mock = Mock<State>()
    let sel = (Selector
                   => IncreaseFailureNode
                   => IncreaseFailureNode
                   => IncreaseFailureNode) |> BuildTree
    let result = Execute sel mock.Object    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    Assert.AreEqual(Failure, result)
    
[<Test>]
let ``Selector Exits Early if Child Succeeds`` () =
    let mock = Mock<State>()
    let sel = Selector
                   => IncreaseSuccessNode
                   => IncreaseFailureNode |> BuildTree
    let result = Execute sel mock.Object
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    Assert.AreEqual (Success, result)
    
[<Test>]
let ``Parallel returns first child result if OneSuccess OneFail``() =
    let mock = Mock<State>()
    let pal = Parallel (ParallelFlag.OneSuccess ||| ParallelFlag.OneFailure)
                => FailureNode => IncreaseSuccessNode |> BuildTree
    let result = Execute pal mock.Object    
    Assert.AreEqual(Failure, result)
    
    let pal2 = Parallel (ParallelFlag.OneSuccess ||| ParallelFlag.OneFailure)
                   => SuccessNode => IncreaseFailureNode |> BuildTree
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Success, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Never)
    
[<Test>]
let ``Parallel returns first Success or result of last child if OneSuccess AllFail``() =
    let mock = Mock<State>()
    let pal = Parallel ParallelFlag.OneSuccess
                  => FailureNode => IncreaseSuccessNode |> BuildTree
    let result = Execute pal mock.Object    
    Assert.AreEqual(Success, result)
    
    let pal2 = Parallel ParallelFlag.OneSuccess
                   => FailureNode => IncreaseFailureNode |> BuildTree
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Failure, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))
    
[<Test>]
let ``Parallel returns first Failure or result of last child if AllSuccess OneFail``() =
    let mock = Mock<State>()
    let pal = Parallel ParallelFlag.OneFailure
                  => FailureNode => IncreaseSuccessNode |> BuildTree
    let result = Execute pal mock.Object    
    Assert.AreEqual(Failure, result)
    
    let pal2 = Parallel ParallelFlag.OneFailure
                   => SuccessNode => IncreaseSuccessNode |> BuildTree
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Success, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(1))
    
[<Test>]
let ``Parallel returns last child if AllSuccess AllFail``() =
    let mock = Mock<State>()
    let pal = Parallel (ParallelFlag.AllFailure ||| ParallelFlag.AllSuccess)
                  => FailureNode => IncreaseSuccessNode |> BuildTree
    let result = Execute pal mock.Object    
    Assert.AreEqual(Success, result)
    
    let pal2 = Parallel (ParallelFlag.AllFailure ||| ParallelFlag.AllSuccess)
                   => SuccessNode => IncreaseFailureNode |> BuildTree
    let result2 = Execute pal2 mock.Object
    Assert.AreEqual(Failure, result2)
    
    mock.Verify((fun x -> x.Increase()), Times.Exactly(2))