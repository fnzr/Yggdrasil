module Yggdrasil.Robot

open Yggdrasil.Messages

type Robot(accountId: uint32, packetWriter, shutdown) =
    member this.AccountId = accountId
    
    member val MaxWeight = 0u with get, set
    
    member val WeightSoftCap = 0 with get, set
    
    member this.AgentProcessor =
        fun (inbox: MailboxProcessor<Message>) ->
            let rec loop() =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                            | Disconnected -> ()
                            | AttributeChange (a, v) ->
                                match a with
                                | Attribute.MaxWeight -> this.MaxWeight <- v
                                | Attribute.Unknown -> ()
                            | Debug -> printfn "%d" this.MaxWeight
                            | WeightSoftCap v -> this.WeightSoftCap <- v
                    return! loop()
                }
            loop()
            
    member public this.Agent = MailboxProcessor.Start(this.AgentProcessor)
