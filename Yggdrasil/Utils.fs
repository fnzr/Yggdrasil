module Yggdrasil.Utils

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.FSharpLu.Json
open NLog

type Message<'a> = {
    Label: string
    Message: 'a
}

type JsonLogger() =
    inherit Logger()
    member this.Send (message,
                      [<CallerMemberName; Optional; DefaultParameterValue("")>] label: string) =
        let msg = Default.serialize {
            Label = label
            Message = message
        }
        base.Info(msg)
        message
