module Yggdrasil.Publisher

open Yggdrasil.PacketTypes
open Yggdrasil.YggrasilTypes


type Subscriptions = Map<int, List<(Agent -> bool) * Agent>>

type PublisherMessages = Publish of Event | Subscribe of Event * (Agent -> bool)
let CreatePublisher() =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<PublisherMessages>) ->
            let rec loop (subscriptions: Subscriptions) =  async {
                let! msg = inbox.Receive()
                
                let next = match msg with
                            | Subscribe (e, filter) ->
                                let list = if subscriptions.ContainsKey <| int e
                                                        then subscriptions.[int e]
                                                        else []
                                subscriptions.Add(int e, list)
                            | Publish e ->
                                let list = if subscriptions.ContainsKey <| int e
                                            then subscriptions.[int e]
                                            else []
                                list
                let subs = if subscriptions.ContainsKey(event.Tag)
                            then 
                                subscriptions                            
                            else subscriptions
                return! loop subscriptions
            }
            loop Map.empty
    )