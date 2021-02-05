module Yggdrasil.Reactive.Monitor

open FSharp.Control.Reactive
open Yggdrasil
open Yggdrasil.Game
open Yggdrasil.Reactive.Persona

type Monitor() =
    let mutable handlers = Map.empty
    let event = Event<_>()
    let _lock = obj()
    [<CLIEvent>]
    member __.Publish = event.Publish
    member __.Observable
        with get() =
            Observable.fromEventPattern "Publish" __
            |> Observable.map (fun e -> e.EventArgs :?> Persona)
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
                    let persona = Persona.Create personaId map
                    let handler = new PersonaHandler(persona, event.Trigger)
                    handlers <- handlers.Add(persona.Id, handler)
                    handler
            handler.Reporter.OnNext msg
        
let agentHandler id msg = ()