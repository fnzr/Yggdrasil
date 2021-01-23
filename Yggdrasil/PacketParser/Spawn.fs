module Yggdrasil.PacketParser.Spawn

open System
open FSharpPlus.Lens
open Yggdrasil.Types
open Yggdrasil.Game
open Yggdrasil.Utils
open Yggdrasil.PacketParser.Decoder

let OnUnitSpawn data (world: World) =
    let (part1, leftover) = MakePartialRecord<UnitRawPart1> data [||]    
    let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
    let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
    let unit = CreateNonPlayer part1 part2 (int x, int y)
    {world with
        NPCs = world.NPCs.Add(unit.Id, unit)}, []   
    
let NonPlayerSpawn = OnUnitSpawn
let PlayerSpawn = OnUnitSpawn

let WalkingUnitSpawn data (world: World) =
    let (part1, leftover) = MakePartialRecord<UnitRawPart1> data [||]
    //skip MoveStartTime: uint32 
    let (part2, _) = MakePartialRecord<UnitRawPart2> (leftover.[4..]) [|24|]
    let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
    let unit = CreateNonPlayer part1 part2 (int x, int y)
    {world with
        NPCs = world.NPCs.Add(unit.Id, unit)}, []

let UnitDisappear data (world: World) =
    let id = ToUInt32 data
    let reason = Enum.Parse(typeof<DisappearReason>, string data.[4]) :?> DisappearReason
    if id = world.Player.Id then
        if reason = DisappearReason.Died then
            setl World._Player
                (Player.withStatus Dead world.Player)
                world, []
        else world, []
    else
        {world with
            NPCs = world.NPCs.Remove(id)
        }, []

