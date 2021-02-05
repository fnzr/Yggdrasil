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
    //Propagators.SetupPropagators game |> ignore

[<EntryPoint>]
let main _ =
    AppDomain.CurrentDomain.FirstChanceException.Add <|
    fun args -> printfn "First change exception: %s: %s" AppDomain.CurrentDomain.FriendlyName args.Exception.Message
    
    //o.Subscribe(printfn "%A") |> ignore
    StartAgent ("roboco", "111111") ()
    Threading.Thread.Sleep 1000
    //o.Subscribe(printfn "late: %A") |> ignore
    Console.ReadKey() |> ignore
    0
