module Yggdrasil.Pipe.Combat

open System
open FSharpPlus.Lens
open NLog
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let ProcessDamage (target: Unit) (source: Unit) (damageInfo: DamageInfo)  world =
    let unit = {target with HP = target.HP - damageInfo.Damage}
    Tracer.Send World.UpdateUnit unit world

//it's really not worth it to refactor this into one function...
//08c8
let DamageDealt2 (info: RawDamageInfo2) callback (world: Game) =
    match world.Units.TryFind info.Source, world.Units.TryFind info.Target with
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
    world
            
//008a
let DamageDealt (info: RawDamageInfo) callback (world: Game) =
    match world.Units.TryFind info.Source, world.Units.TryFind info.Target with
    | None, _ | _, None -> Logger.Error "Failed loading units to apply damage"
    | Some source, Some target ->
        let damage = {
            Damage = info.Damage
            Type = info.Type
        }
        let mutable delay = int <| (int64 info.Tick) - Connection.Tick()
        if delay < 0 then delay <- 0
        Delay (fun _ ->  callback <| ProcessDamage target source damage) delay
    world
    
let UpdateMonsterHP (info: MonsterHPInfo) (world: Game) =
    match world.Units.TryFind info.aid with
    | Some unit ->
        Tracer.Send World.UpdateUnit {unit with HP = info.HP; MaxHP = info.MaxHP} world
    | None -> Logger.Warn ("Unhandled HP update for {aid}", info.aid); world
    
//I assume the skill effect comes in another packet...
let ClearSkill (actionId: Guid) (sourceId: uint32) (targetId: uint32) (skillCast: SkillCast) (world: Game) =
    let w1 =
        match world.Units.TryFind sourceId with
        | None -> world
        | Some source ->
            if source.ActionId = actionId then
                Tracer.Send World.UpdateUnit {source with Status = Idle} world
            else world
    match world.Units.TryFind targetId with
    | None -> w1
    | Some target ->
        let filter = fun (s: SkillCast, u: Unit) ->
            s.SkillId <> skillCast.SkillId || u.Id <> targetId
        Tracer.Send World.UpdateUnit
            {target with TargetOfSkills = List.filter filter target.TargetOfSkills}
        <| w1
            
    
let SkillCast castRaw callback (world: Game) =
    match (world.Units.TryFind castRaw.source, world.Units.TryFind castRaw.target) with
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
                Tracer.Send World.UpdateUnit {target with TargetOfSkills = (cast, caster) :: target.TargetOfSkills} w1
            do! Async.Sleep (int castRaw.delay)
            callback <| ClearSkill id caster.Id target.Id cast
        }
    world

let AddSkills skills (world: Game) =
    Tracer.Send <|
        {world with
            Skills = {world.Skills with
                        List = List.append world.Skills.List skills}}
