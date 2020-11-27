module Yggdrasil.Mailbox.Publisher

open System
open Yggdrasil.Communication
open Yggdrasil.YggrasilTypes

type Subscriber = Type * (Agent -> bool) * MailboxProcessor<AgentEvent * Agent>
type Supervisor = (AgentEvent * Agent -> bool) * MailboxProcessor<AgentEvent * Agent>
type PublisherMessages =
    | Publish of AgentEvent * Agent
    | Subscribe of Subscriber
    | Supervise of Supervisor
    
type PublisherMailbox = MailboxProcessor<PublisherMessages>


let private MaybePublishEvent (event: AgentEvent) agent (subscription: Subscriber) =
    let (type_, filter, mailbox) =  subscription
    if type_ = event.GetType() && filter(agent)
    then mailbox.Post <| (event, agent) |> ignore; true
    else false
    
let private PostToSupervisor event agent (supervisor: Supervisor) =
    let (filter, mailbox) = supervisor
    if filter (event, agent)
    then mailbox.Post(event, agent)
    else ()
        
let private PartitionAndPublish subscribers event agent =
    let rec loop subs remaining =
        match subs with
        | [] -> remaining
        | head :: tail ->
            if MaybePublishEvent event agent head
            then loop tail remaining
            else loop tail (head :: remaining)
    loop subscribers []        

let CreatePublisherMailbox() =
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