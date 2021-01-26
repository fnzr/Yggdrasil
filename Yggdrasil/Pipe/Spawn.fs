module Yggdrasil.Pipe.Spawn

open FSharpPlus.Lens
open NLog
open Yggdrasil.Types
open Yggdrasil.Game
open Yggdrasil.Utils
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let UnitSpawn (npc: Unit) (world: Game) =
    Tracer.Send {world with
                    Units = world.Units.Add(npc.Id, npc)}  

let UnitDisappear id reason (world: Game) =
    if id = world.PlayerId then
        if reason = DisappearReason.Died then
            Tracer.Send <| world.UpdateUnit {world.Player with Status = Dead}
        else world
    else
        Tracer.Send <| {world with Units = world.Units.Remove(id)}

