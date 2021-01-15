open System
open System.Net
open NLog
open Yggdrasil.Behavior.Machines
open Yggdrasil.Behavior.Behavior
let Logger = LogManager.GetLogger("Sunna")

let Pack x0 y0 x1 y1 =
    [|
        (byte) (x0 >>> 2)
        (byte) ((x0 <<< 6) ||| ((y0>>>4)&&&0x3f))
        (byte) ((y0 <<< 4) ||| ((x1>>>6)&&&0x0f))
        (byte) ((x1 <<< 2) ||| ((y1>>>8)&&&0x03))
        (byte) y1        
    |]

let UnpackPosition2 (data: byte[]) =
    int16 (data.[0] <<< 2) ||| int16 (data.[1] >>> 6),  //X0
    (int16 (data.[1] <<< 2)) <<< 2 ||| int16 (data.[2] >>> 4), //Y0
    int16 (data.[2] <<< 6) ||| int16 (data.[3] >>> 2),  //X1
    (int16 (data.[3] >>> 3 <<< 3) <<< 1) ||| int16 (data.[4] <<< 3 >>> 3),  //Y1
    (data.[5] >>> 4),  //dirX
    (data.[5] <<< 4) //dirY
    
    

[<EntryPoint>]
let main argv =
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let machine = DefaultMachine.Create server "roboco" "111111" 
    StartAgent machine
    //printfn "%A" <| BuildUnionKey b
    //printfn "%A" (string (FSharpValue.GetUnionFields(a, a.GetType())))
    //let (t, os) = FSharpValue.GetUnionFields(b, b.GetType())
    //let (t2, os2) = FSharpValue.GetUnionFields(os.[0], os.[0].GetType())
    //let (t3, os3) = FSharpValue.GetUnionFields(os2.[0], os2.[0].GetType())
    //printfn "%A" <| FSharpValue.GetUnionFields(os3.[0], os3.[0].GetType())
    //printfn "%s" (string b)
    Console.ReadKey() |> ignore 
    0
