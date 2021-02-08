module Yggdrasil.IO.Decoder

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open Microsoft.FSharp.Reflection
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

let MakePartialRecord<'T> (data: byte[]) (stringSizes: int[]) =
    let queue = Queue<obj>()
    let fields = typeof<'T>.GetProperties()
    let rec loop (properties: PropertyInfo[]) (data: byte[]) (stringSizes: int[]) =
        match properties with
        | [||] -> FSharpValue.MakeRecord(typeof<'T>, queue.ToArray()) :?> 'T, data
        | _ ->
            let property = properties.[0]
            let size, stringsS = if property.PropertyType = typeof<string>
                                    then stringSizes.[0], stringSizes.[1..]
                                    else Marshal.SizeOf(property.PropertyType), stringSizes
            
            let value = if property.PropertyType = typeof<int32> then ToInt32 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint32> then ToUInt32 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<byte> then data.[0] :> obj
                        elif property.PropertyType = typeof<int16> then ToInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint16> then ToUInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<string> then ToString data.[..size-1] :> obj
                        else raise <| invalidArg (string property.Name)
                                          (sprintf "Unhandled type: %A" property.PropertyType)
            queue.Enqueue(value);
            loop properties.[1..] data.[size..] stringsS    
    loop fields data stringSizes
    
let MakeRecord<'T> data = fst <| MakePartialRecord<'T> data [||]
let UnpackPosition (data: byte[]) =
    (int16 (data.[0] <<< 2)) ||| int16 (data.[1]&&&(~~~0x3fuy) >>> 6), //x
    ((int16 (data.[1]&&&0x3fuy)) <<< 4) ||| (int16 (data.[2]>>>4)), //y
    data.[2] <<< 4 //not sure about this //Direction
    
let UnpackPosition2 (data: byte[]) =
    (int16 (data.[0] <<< 2)) ||| int16 (data.[1]&&&(~~~0x3fuy) >>> 6), //x0
    ((int16 (data.[1]&&&0x3fuy)) <<<4) ||| (int16 (data.[2]>>>4)), //y0
    (int16 (data.[2]&&&(0x0fuy)) <<< 6) ||| (int16 (data.[3]&&&(~~~0x03uy)) >>> 2), //x1
    (int16 (data.[3]&&&0x3uy) <<< 8) ||| int16 data.[4], // y1   
    (data.[5] >>> 4),  //dirX
    (data.[5] <<< 4) //dirY