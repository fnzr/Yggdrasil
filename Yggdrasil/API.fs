module Yggdrasil.API

open System
open System.Collections.Generic
open System.IO
open System.Net.Sockets
open System.Reflection
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Agent
open Yggdrasil.Behavior
open Yggdrasil.IO
open Yggdrasil.Types

let Logger = LogManager.GetLogger("API")

    
let ArgumentConverter (value: string) target =
    if target = typeof<Parameter>
    then Enum.Parse(typeof<Parameter>, value)
    else Convert.ChangeType(value, target)
    
let FindUnionCase cases name =
    Array.tryFind
        (fun (u: UnionCaseInfo) ->
                String.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase))
        cases

type MailboxesMessage = | Help | List | Select | Exit | Char of string
let MailboxesMessageCases = FSharpType.GetUnionCases(typeof<MailboxesMessage>)
    
let MakeMessage<'T> (case: UnionCaseInfo) (args: string[]) =
        let convert = fun i (p: PropertyInfo) -> ArgumentConverter args.[i] p.PropertyType
        let values = Array.mapi convert <| case.GetFields()    
        FSharpValue.MakeUnion(case, values) :?> 'T
