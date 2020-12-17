// Learn more about F# at http://fsharp.org

open System
open Moq
open Yggdrasil.BehaviorTree

let a (v: S) = v.Increase(); Failure
let Tree = Sequence([|Action(a); Action(a); Action(a)|])

[<EntryPoint>]
let main argv =
    let mock = Mock<S>()
    let latestQ = Run Tree mock.Object 
    mock.Verify((fun x -> x.Increase()), Times.Exactly(3))
    //printfn "%A" mock.Object
    0 // return an integer exit code
