module Yggdrasil.Pipe.Spawn

open FSharpPlus.Lens
open NLog
open Yggdrasil.Types
open Yggdrasil.Game
open Yggdrasil.Utils
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let UnitSpawn (npc: Unit) (game: Game) =
    Tracer.Send {game with
                    Units = game.Units.Add(npc.Id, npc)}  

let UnitDisappear id reason (game: Game) =
    if id = game.PlayerId then
        if reason = DisappearReason.Died then
            Tracer.Send <| game.UpdateUnit {game.Player with Status = Dead}
        else game
    else
        Tracer.Send <| {game with Units = game.Units.Remove(id)}

