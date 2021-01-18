module Yggdrasil.PacketParser.Combat

open System
open Yggdrasil.Game.Skill
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
open Yggdrasil.PacketParser.Decoder

let ProcessDamage (target: Unit) (source: Unit) (damageInfo: DamageInfo)  world =
    let unit = {target with HP = target.HP - damageInfo.Damage}
    World.UpdateUnit unit world, [||]

//it's really not worth it to refactor this into one function...
//08c8
let DamageDealt2 data callback (world: World) =
    let info = MakeRecord<RawDamageInfo2> data
    match World.Unit world info.Source, World.Unit world info.Target with
    | None, _ | _, None -> Logger.Error "Failed loading units to apply damage"
    | Some source, Some target ->
        if info.IsSPDamage > 0uy then Logger.Warn "Unhandled SP damage"
        else
            let damage = {
                Damage = info.Damage
                Type = info.Type
            }
            let mutable delay = int <| (int64 info.Tick) - Connection.Tick()
            if delay < 0 then delay <- 0
            Delay (fun _ ->  callback <| ProcessDamage target source damage) delay
    world, [||]
            
//008a
let DamageDealt data callback (world: World) =
    let info = MakeRecord<RawDamageInfo> data
    match World.Unit world info.Source, World.Unit world info.Target with
    | None, _ | _, None -> Logger.Error "Failed loading units to apply damage"
    | Some source, Some target ->
        let damage = {
            Damage = info.Damage
            Type = info.Type
        }
        let mutable delay = int <| (int64 info.Tick) - Connection.Tick()
        if delay < 0 then delay <- 0
        Delay (fun _ ->  callback <| ProcessDamage target source damage) delay
    world, [||]
    
let UpdateMonsterHP data (world: World) =
    let info = MakeRecord<MonsterHPInfo> data
    match World.Unit world info.aid with
    | Some unit ->
        World.UpdateUnit {unit with HP = info.HP; MaxHP = info.MaxHP} world, [||]
    | None -> Logger.Warn ("Unhandled HP update for {aid}", info.aid); world, [||]
    
//I assume the skill effect comes in another packet...
let ClearSkill (actionId: Guid) (sourceId: uint32) (targetId: uint32) (skillCast: SkillCast) (world: World) =
    let w1 =
        match World.Unit world sourceId with
        | None -> world
        | Some source ->
            if source.ActionId = actionId then
                World.UpdateUnit {source with Status = Idle} world
            else world
    match World.Unit world targetId with
    | None -> w1, [||]
    | Some target ->
        let filter = fun (s: SkillCast, u: Unit) ->
            s.SkillId <> skillCast.SkillId || u.Id <> targetId
        World.UpdateUnit {target
                          with TargetOfSkills = List.filter filter target.TargetOfSkills}
        <| w1, [||]
            
    
let SkillCast data callback (world: World) =
    let castRaw = MakeRecord<RawSkillCast> data    
    match (World.Unit world castRaw.source, World.Unit world castRaw.target) with
    | (None, _) | (_, None) -> Logger.Warn "Missing skill cast units!"
    | (Some caster, Some target) ->
        let cast: SkillCast = {
            SkillId = castRaw.skillId
            Property = castRaw.property
        }
        Async.Start <| async {
            let id = Guid.NewGuid()
            callback <| fun world ->
                let w1 = World.UpdateUnit {caster with ActionId = id; Status = Casting} world
                World.UpdateUnit {target with TargetOfSkills = (cast, caster) :: target.TargetOfSkills} w1, [||]
            do! Async.Sleep (int castRaw.delay)
            callback <| ClearSkill id caster.Id target.Id cast
        }
    world, [||]