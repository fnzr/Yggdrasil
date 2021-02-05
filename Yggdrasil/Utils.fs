module Yggdrasil.Utils

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text
open Microsoft.FSharpLu.Json
open Microsoft.FSharpLu.Json
open NLog
open Yggdrasil.Types

let ToUInt16 data = BitConverter.ToUInt16(data, 0)
let ToInt16 data = BitConverter.ToInt16(data, 0)
let ToUInt32 data = BitConverter.ToUInt32(data, 0)
let ToInt32 data = BitConverter.ToInt32(data, 0)
let ToInt64 data = BitConverter.ToInt64(data, 0)
let ToChar data = BitConverter.ToChar(data, 0)
let ToBool data = BitConverter.ToBoolean(data, 0)
let ToParameter data : Parameter = data |> ToUInt16 |> LanguagePrimitives.EnumOfValue
let ToString (data: byte[]) = (data |> Encoding.UTF8.GetString).Trim [| '\x00'; ''; '�'; '\000'; '\127' |]

let FillBytes (data:string) size =
    Array.concat([|
        Encoding.UTF8.GetBytes(data);
        Array.zeroCreate (size - data.Length)
   |])
    
let SetValue (logger: Logger) (field: byref<'T>) (value: 'T) (description: string) =
    if System.Collections.Generic.EqualityComparer.Default.Equals(field, value) then false
    else
        logger.Debug("{event}: {value}", description, value)
        field <- value
        true
        
let Delay fn (delay: int) = Async.Start <| async {
    do! Async.Sleep delay
    fn()
}

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

module Hex =
    
        [<CompiledName("ToHexDigit")>]
        let toHexDigit n =
            if n < 10 then char (n + 0x30) else char (n + 0x37)
    
        [<CompiledName("FromHexDigit")>]
        let fromHexDigit c =
            if c >= '0' && c <= '9' then int c - int '0'
            elif c >= 'A' && c <= 'F' then (int c - int 'A') + 10
            elif c >= 'a' && c <= 'f' then (int c - int 'a') + 10
            else raise <| ArgumentException()
        
        [<CompiledName("Encode")>]
        let encode (buf:byte array) (prefix:bool) =
            let hex = Array.zeroCreate (buf.Length * 2)
            let mutable n = 0
            for i = 0 to buf.Length - 1 do
                hex.[n] <- toHexDigit ((int buf.[i] &&& 0xF0) >>> 4)
                n <- n + 1
                hex.[n] <- toHexDigit (int buf.[i] &&& 0xF)
                n <- n + 1
            if prefix then String.Concat("0x", String(hex)) 
            else String(hex)
            
        [<CompiledName("Decode")>]
        let decode (s:string) =
            match s with
            | null -> nullArg "s"
            | _ when s.Length = 0 -> Array.empty
            | _ ->
                let mutable len = s.Length
                let mutable i = 0
                if len >= 2 && s.[0] = '0' && (s.[1] = 'x' || s.[1] = 'X') then do
                    len <- len - 2
                    i <- i + 2
                if len % 2 <> 0 then invalidArg "s" "Invalid hex format"
                else
                    let buf = Array.zeroCreate (len / 2)
                    let mutable n = 0
                    while i < s.Length do
                        buf.[n] <- byte (((fromHexDigit s.[i]) <<< 4) ||| (fromHexDigit s.[i + 1]))
                        i <- i + 2
                        n <- n + 1
                    buf
