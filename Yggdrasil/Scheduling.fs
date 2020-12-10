module Yggdrasil.Scheduling

open System.Diagnostics

let Stopwatch = Stopwatch()
Stopwatch.Start()
let GetCurrentTick() = Stopwatch.ElapsedMilliseconds
