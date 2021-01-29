module Yggdrasil.Pipe.Combat

open System
open FSharpPlus.Lens
open NLog
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let ProcessDamage (target: Unit) (source: Unit) (damageInfo: DamageInfo)  game =
    let unit = {target with HP = target.HP - damageInfo.Damage}
    Tracer.Send Game.UpdateUnit unit game

//it's really not worth it to refactor this into one function...
//08c8
let DamageDealt2 (info: RawDamageInfo2) callback (game: Game) =
    match game.Units.TryFind info.Source, game.Units.TryFind info.Target with
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
    game
            
//008a
let DamageDealt (info: RawDamageInfo) callback (game: Game) =
    match game.Units.TryFind info.Source, game.Units.TryFind info.Target with
    | None, _ | _, None -> Logger.Error "Failed loading units to apply damage"
    | Some source, Some target ->
        let damage = {
            Damage = info.Damage
            Type = info.Type
        }
        let mutable delay = int <| (int64 info.Tick) - Connection.Tick()
        if delay < 0 then delay <- 0
        Delay (fun _ ->  callback <| ProcessDamage target source damage) delay
    game
    
let UpdateMonsterHP (info: MonsterHPInfo) (game: Game) =
    match game.Units.TryFind info.aid with
    | Some unit ->
        Tracer.Send Game.UpdateUnit {unit with HP = info.HP; MaxHP = info.MaxHP} game
    | None -> Logger.Warn ("Unhandled HP update for {aid}", info.aid); game
    
//I assume the skill effect comes in another packet...
let ClearSkill action (sourceId: uint32) (targetId: uint32) (skillCast: SkillCast) (game: Game) =
    let w1 =
        match game.Units.TryFind sourceId with
        | None -> game
        | Some source ->
            if source.Action = action then
                Tracer.Send Game.UpdateUnit {source with Action = Idle} game
            else game
    match game.Units.TryFind targetId with
    | None -> w1
    | Some target ->
        let filter = fun (s: SkillCast, u: Unit) ->
            s.SkillId <> skillCast.SkillId || u.Id <> targetId
        Tracer.Send Game.UpdateUnit
            {target with TargetOfSkills = List.filter filter target.TargetOfSkills}
        <| w1
            
    
let SkillCast castRaw callback (game: Game) =
    match (game.Units.TryFind castRaw.source, game.Units.TryFind castRaw.target) with
    | (None, _) | (_, None) ->
        Logger.Warn "Missing skill cast units!"; game
    | (Some caster, Some target) ->
        let cast: SkillCast = {
            SkillId = castRaw.skillId
            Property = castRaw.property
        }
        let action = Casting
        Async.Start <| async {            
            do! Async.Sleep (int castRaw.delay)
            callback <| ClearSkill action caster.Id target.Id cast
        }
        Tracer.Send Game.UpdateUnit {caster with Action = action} game

let AddSkills skills (game: Game) =
    Tracer.Send <|
        {game with
            Skills = {game.Skills with
                        List = List.append game.Skills.List skills}}
