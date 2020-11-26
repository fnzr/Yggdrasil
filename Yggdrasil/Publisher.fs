module Yggdrasil.Publisher

open System
open Yggdrasil.YggrasilTypes


type Subscriber = Type * (Agent -> bool) * MailboxProcessor<Event * Agent>
type Supervisor = (Event * Agent -> bool) * MailboxProcessor<Event * Agent>
     

let MaybePublishEvent (event: Event) agent (subscription: Subscriber) =
    let (type_, filter, mailbox) =  subscription
    if type_ = event.GetType() && filter(agent)
    then mailbox.Post <| (event, agent) |> ignore; true
    else false
    
let PostToSupervisor event agent (supervisor: Supervisor) =
    let (filter, mailbox) = supervisor
    if filter (event, agent)
    then mailbox.Post(event, agent)
    else ()
    
    
let PartitionAndPublish subscribers event agent =
    let rec loop subs remaining =
        match subs with
        | [] -> remaining
        | head :: tail ->
            if MaybePublishEvent event agent head
            then loop tail remaining
            else loop tail (head :: remaining)
    loop subscribers []        
        
    
type PublisherMessages =
    | Publish of Event * Agent
    | Subscribe of Subscriber
    | Supervise of Supervisor 

let CreatePublisher() =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor<PublisherMessages>) ->
            let rec loop (subscribers, supervisors) =  async {
                let! msg = inbox.Receive()
                let next = match msg with
                            | Subscribe (e, f, m) -> (e, f, m) :: subscribers, supervisors
                            | Supervise (f, m) -> subscribers, (f, m) :: supervisors
                            | Publish (e, a) ->
                                List.iter (PostToSupervisor e a) supervisors;
                                PartitionAndPublish subscribers e a, supervisors
                return! loop next
            }
            loop (List.empty, List.empty)
    )