module Yggdrasil.Pipe.Spawn

open FSharpPlus.Lens
open Yggdrasil.Types
open Yggdrasil.Game

let UnitSpawn (npc: NonPlayer) (world: World) =
    {world with
        NPCs = world.NPCs.Add(npc.Id, npc)}  

let UnitDisappear id reason (world: World) =
    if id = world.Player.Id then
        if reason = DisappearReason.Died then
            setl World._Player
                (Player.withStatus Dead world.Player)
                world
        else world
    else
        {world with
            NPCs = world.NPCs.Remove(id)
        }

