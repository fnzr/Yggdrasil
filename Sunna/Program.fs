open System
open System.Net
open NLog
let Logger = LogManager.GetLogger("Sunna")

type E = interface end
type BranchA = A1 of int | A2 | A3 of (int * int)
                interface E
type BranchB = B1 of string
                interface E
type Root = BranchA of BranchA | BranchB of BranchB
            interface E

[<EntryPoint>]
let main argv =
    //let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    //let machine = DefaultMachine.Create server "roboco" "111111" 
    //StartAgent machine
    //Console.ReadKey() |> ignore
    0
