module Yggdrasil.Behavior.Propagators

open System.Reactive
open Yggdrasil
open FSharp.Control.Reactive

let PlayerId = Subject.behavior 0u

let PlayerIdMapper = Observable.filter(fun i -> printfn "Observed: %A" i; false) PlayerId

let PlayerIdWatcher = Observable.add (fun i -> printfn "22: %A" i;) PlayerIdMapper