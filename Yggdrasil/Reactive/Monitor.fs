module Yggdrasil.Reactive.Monitor

open System.Collections.Generic
open FSharp.Control.Reactive
open FSharpx.Collections
open Yggdrasil
open Yggdrasil.Game
open Yggdrasil.Reactive.Persona
open Yggdrasil.Types

type Report =
    {
        Map: string
        TargetId: Id
        Message: MonitorMessage
    }

let CreateHandler persona onUpdate = new EntityHandler(persona, onUpdate)

let MonitorMailbox onEntityUpdate =
    MailboxProcessor.Start
    <| fun (inbox: MailboxProcessor<_>) ->
        let handlers = Dictionary<Id, EntityHandler>()
        let rec loop () = async {
            let! report = inbox.Receive()
            printfn "%A" report.Message
            match handlers.TryGetValue report.TargetId with                
                | (true, handler) -> handler.Reporter.OnNext report.Message
                | (false, _) ->
                    match report.Message with
                    | NewPlayer entity ->
                        handlers.Add(entity.Id, new EntityHandler(entity, onEntityUpdate))
                    | NewUnit unit ->                            
                        let entity = Entity.FromUnit unit report.Map
                        let handler = CreateHandler entity onEntityUpdate
                        handlers.Add(entity.Id, handler)
                    | _ -> invalidArg (string report.TargetId) "Unknown unit"
            return! loop()
        }
        loop()

type Monitor() =
    let mutable handlers = Map.empty
    let event = Event<_>()
    let _lock = obj()
    [<CLIEvent>]
    member __.Publish = event.Publish
    member __.Observable
        with get() =
            Observable.fromEventPattern "Publish" __
            |> Observable.map (fun e -> e.EventArgs :?> Entity)
            |> Observable.distinctUntilChanged
            
    member __.Push map (personaId: Types.Id) (msg: MonitorMessage) =
        lock _lock
        <| fun _ ->
            let handler =
                //TODO: handle map changed
                printfn "%A" msg
                match handlers.TryFind personaId with
                | Some handler -> handler
                | None ->
                    let persona = Entity.Create personaId map
                    let handler = new EntityHandler(persona, event.Trigger)
                    handlers <- handlers.Add(persona.Id, handler)
                    handler
            handler.Reporter.OnNext msg
        
let agentHandler id msg = ()