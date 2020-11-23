module Yggdrasil.Robot

open Microsoft.FSharp.Reflection
open Yggdrasil.Structure

type Robot(accountId: uint32) =
    member public this.AccountId = accountId
    
    member val WeightSoftCap = 0 with get, set
    member val WeightHardCap = 0 with get, set
    
    member val ParameterMap = Map.empty with get, set
    member val ParameterLongMap = Map.empty with get, set
    
    //dunno if I want a list here
    member val Units = [] with get, set
    
    member val Skills = [] with get, set
    
    member this.AgentProcessor =
        fun (inbox: MailboxProcessor<Message>) ->
            let rec loop() =
                async {
                    let! msg = inbox.Receive()
                    //Logging.LogMessage this.AccountId msg
                    match msg with
                            | Disconnected -> ()
                            | ParameterChange (p, v) -> this.ParameterMap <- this.ParameterMap.Add (p.ToString(), v)
                            | ParameterLongChange (p, v) -> this.ParameterLongMap <- this.ParameterLongMap.Add (p.ToString(), v)
                            | Debug -> printfn "Debug"
                            | SpawnNPC u -> this.Units <- u :: this.Units
                            | AddSkill s -> this.Skills <- s :: this.Skills
                            | WeightSoftCap v -> this.WeightSoftCap <- v
                    return! loop()
                }
            loop()
            
    member public this.Agent = MailboxProcessor.Start(this.AgentProcessor)
