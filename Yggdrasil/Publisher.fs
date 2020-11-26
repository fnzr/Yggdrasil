module Yggdrasil.Publisher

open System
open Yggdrasil.PacketTypes
open Yggdrasil.YggrasilTypes


type Subscriber = Type * (Agent -> bool) * MailboxProcessor<Event * Agent>
type Subscriptions = List<Subscriber>

let FilterSubscriptions expected agent (subscription: Subscriber) =
    let (type_, filter, _) =  subscription
    type_ = expected && filter(agent)
    
type PublisherMessages = Publish of Event * Agent | Subscribe of Subscriber

let CreatePublisher() =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<PublisherMessages>) ->
            let rec loop (subscriptions: Subscriptions) =  async {
                let! msg = inbox.Receive()
                let next = match msg with
                            | Subscribe (e, f, m) -> (e, f, m) :: subscriptions
                            | Publish (e, a) ->
                                let filter = FilterSubscriptions (e.GetType()) a
                                let matches, remaining = List.partition filter subscriptions
                                List.iter (fun ((_, _, m): Subscriber) -> m.Post(e,a)) matches
                                remaining
                return! loop next
            }
            loop List.empty
    )