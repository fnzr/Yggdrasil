module Yggdrasil.Robot

open Microsoft.FSharp.Reflection
open Yggdrasil.Structure

let rec Aggregate (state: Map<string, uint32>) (array: List<string>) =
    match array with
    | head :: tail -> Aggregate (state.Add(head, 0u)) tail
    | [] -> state
    
let AttributeMap = Aggregate Map.empty
                    (FSharpType.GetUnionCases typeof<CharacterAttribute>
                        |> Array.toList |> List.map (fun i -> i.Name))

type Robot(accountId: uint32, packetWriter, shutdown) =
    member public this.AccountId = accountId
    
    member val WeightSoftCap = 0 with get, set
    
    member val AttributeMap = Map.map (fun _ v -> v) AttributeMap with get, set
    
    member this.AgentProcessor =
        fun (inbox: MailboxProcessor<Message>) ->
            let rec loop() =
                async {
                    let! msg = inbox.Receive()
                    Logging.LogMessage this.AccountId msg
                    match msg with
                            | Disconnected -> ()
                            | AttributeChange (a, v) -> this.AttributeMap <- this.AttributeMap.Add (a.ToString(), v)                                
                            | Debug -> printfn "Debug"
                            | WeightSoftCap v -> this.WeightSoftCap <- v
                    return! loop()
                }
            loop()
            
    member public this.Agent = MailboxProcessor.Start(this.AgentProcessor)
