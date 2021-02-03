module Sunna.Main
open System
open System.Net
open System.Text
open FSharp.Control.Reactive
open Yggdrasil.Behavior
open Yggdrasil.Game
open Sunna.Machines
open Yggdrasil.Game.Components
open FSharp.Control.Reactive.Builders
open Yggdrasil.IO

let StartAgent credentials initialMachineState =
    let server = IPEndPoint (IPAddress.Parse "127.0.0.1", 6900)
    let game = Handshake.Login server credentials
    printfn "Game started! %A" game
    Propagators.SetupPropagators game |> ignore

type Action = Move | Teleport | Attack
type Pre = HasMoveTarget | IsIdle | HasMana | CanMove | HasTarget
type Post = AtMoveTarget | Attacked

type Specification = {
    Action: Action
    Pre: Pre list
    Post: Post list
}

let moveSpec = {
    Action = Move
    Pre = [HasMoveTarget; IsIdle; CanMove]
    Post = [AtMoveTarget]
}

let teleportSpec = {
    Action = Teleport
    Pre = [HasMoveTarget; HasMana]
    Post = [AtMoveTarget]
}

let otherSpec = {
    Action = Attack
    Pre = [HasTarget]
    Post = [Attacked]
}

let specs = [moveSpec; teleportSpec; otherSpec]
let postCases = [AtMoveTarget; Attacked]

let buildPreTree spec =
    let a = 
        List.fold (fun (s: StringBuilder) p -> s.Append($"{string p};")) (StringBuilder "Sequence: ") spec.Pre
    $"{spec.Action};" |> a.Append |> string
let r =
    List.map
    <| (fun post ->
            List.fold
            <| fun (s: StringBuilder) e ->
                if List.contains post e.Post then s.Append(buildPreTree e)
                else s
            <| (StringBuilder $"Selector: {post};")
            <| specs)
    <| postCases
    
let gameUpdates () =
    observe {
        yield UnitPosition (1u, (1s, 1s))
        yield UnitPosition (1u, (2s, 2s))
        yield UnitPosition (1u, (3s, 3s))
    }

List.iter (printfn "%A") r
[<EntryPoint>]
let main _ =
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args -> printfn "First change exception: %s: %s" AppDomain.CurrentDomain.FriendlyName args.Exception.Message
    
    let ob = gameUpdates()
    let posObs unitId =
        (Observable.choose
            <| fun e ->
            match e with
            | UnitPosition (id, pos) -> if id = unitId then Some pos else None
            | _ -> None
        <| ob)
    //o.Subscribe(printfn "%A") |> ignore
    //StartAgent ("roboco", "111111") ()
    Threading.Thread.Sleep 1000
    //o.Subscribe(printfn "late: %A") |> ignore
    Console.ReadKey() |> ignore
    0
