module Yggdrasil.Pipe.Spawn

open FSharpPlus.Lens
open NLog
open Yggdrasil.Types
open Yggdrasil.Game
open Yggdrasil.Utils
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let UnitSpawn (npc: NonPlayer) (world: World) =
    Tracer.Send {world with
                    NPCs = world.NPCs.Add(npc.Id, npc)}  

let UnitDisappear id reason (world: World) =
    if id = world.Player.Id then
        if reason = DisappearReason.Died then
            Tracer.Send <|
            setl World._Player
                (Player.withStatus Dead world.Player)
                world
        else world
    else
        Tracer.Send <|
        {world with
            NPCs = world.NPCs.Remove(id)
        }

