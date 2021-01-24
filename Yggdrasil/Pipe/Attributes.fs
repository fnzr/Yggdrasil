module Yggdrasil.Pipe.Attributes

open FSharpPlus.Lens
open Yggdrasil.Types
open Yggdrasil.Game
open Yggdrasil.Utils

let OnU32ParameterUpdate code (value: uint32) (world: World) =
    if code = Parameter.MaxWeight then
        let i = {world.Player.Inventory with MaxWeight = value}
        setl World._Player {world.Player with Inventory = i} world
    else if code = Parameter.Weight then
        setl World._Player (setl Player._Weight value world.Player) world
    else
        //| Parameter.SkillPoints -> player.Level.SkillPoints <- value
        //| Parameter.JobLevel -> player.Level.JobLevel <- value
        //| Parameter.BaseLevel -> player.Level.BaseLevel <- value
        let player = world.Player
        setl World._Player <|
            (match code with
                | Parameter.MaxHP -> setl Player._MaxHP (int value) player
                | Parameter.HP -> setl Player._HP (int value) player
                | Parameter.MaxSP -> setl Player._MaxSP (int16 value) player
                | Parameter.SP -> setl Player._SP (int16 value) player
                | _ -> player) 
        <| world   
    
let OnI16ParameterUpdate code value (world: World) =
    let bp = world.Player.BattleParameters
    setl World._Player <|
        setl Player._BattleParameters 
            (match code with
            | Parameter.Hit -> {bp with Hit = value}
            | Parameter.Flee1 -> {bp with Flee1 = value}
            | Parameter.Flee2 -> {bp with Flee2 = value}
            | Parameter.Critical -> {bp with Critical = value}
            | _ -> bp)
            world.Player
    <| world
    
let OnU16ParameterUpdate code value (world: World) =
    setl World._Player <|
    if code = Parameter.Speed then
        setl Player._Unit {world.Player.Unit with Speed = int16 value} world.Player
    else
        let bp = world.Player.BattleParameters        
        setl Player._BattleParameters
            (match code with    
            | Parameter.AttackSpeed -> {bp with AttackSpeed = value}
            | Parameter.Attack1 -> {bp with Attack1 = int16 value}
            | Parameter.Attack2 -> {bp with Attack2 = int16 value}
            | Parameter.Defense1 -> {bp with Defense1 = int16 value}
            | Parameter.Defense2 -> {bp with Defense2 = int16 value}
            | Parameter.MagicAttack1 -> {bp with MagicAttack1 = int16 value}
            | Parameter.MagicAttack2 -> {bp with MagicAttack2 = int16 value}
            | Parameter.MagicDefense1 -> {bp with MagicDefense1 = int16 value}
            | Parameter.MagicDefense2 -> {bp with MagicDefense2 = int16 value}
            | Parameter.AttackRange -> {bp with AttackRange = value}
            | _ -> bp)
        <| world.Player
    <| world
        
    
let OnI32ParameterUpdate code value (world: World) =
    if code = Parameter.Zeny then
        setl World._Player (setl Player._Zeny value world.Player) world
    else 
        let attributes = world.Player.Attributes.Primary
        setl World._Player <|
            setl Player._PrimaryAttributes
                (match code with
                    | Parameter.USTR -> {attributes with STR = int16 value}
                    | Parameter.UAGI -> {attributes with AGI = int16 value}
                    | Parameter.UDEX -> {attributes with DEX = int16 value}
                    | Parameter.UVIT -> {attributes with VIT = int16 value}
                    | Parameter.ULUK -> {attributes with LUK = int16 value}
                    | Parameter.UINT -> {attributes with INT = int16 value}
                    | _ -> attributes)
                world.Player
        <| world
        
let On64ParameterUpdate code value (world: World) =
    let player = world.Player
    match code with
        | Parameter.BaseExp -> player.Level.BaseExp <- value
        | Parameter.JobExp -> player.Level.JobExp <- value
        | Parameter.NextBaseExp -> player.Level.NextBaseExp <- value
        | Parameter.NextJobExp -> player.Level.NextJobExp <- value
        | _ -> ()
    world
    
let OnPairParameterUpdate code (value, plus) (world: World) =
    let primary = world.Player.Attributes.Primary
    let bonus = world.Player.Attributes.Bonus
    let p, b =
        match code with
            | Parameter.STR ->
                {primary with STR = value}, {bonus with STR = plus}
            | Parameter.AGI ->
                {primary with AGI = value}, {bonus with AGI = plus}
            | Parameter.DEX ->
                {primary with DEX = value}, {bonus with DEX = plus}
            | Parameter.VIT ->
                {primary with VIT = value}, {bonus with VIT = plus}
            | Parameter.LUK ->
                {primary with LUK = value}, {bonus with LUK = plus}
            | Parameter.INT ->
                {primary with INT = value}, {bonus with INT = plus}
            | _ -> (primary, bonus)
    setl World._Player <|
        setl Player._Attributes
            {world.Player.Attributes
             with Primary = p; Bonus = b} world.Player        
    <| world

let ParameterChange parameter value (world: World) =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> OnU32ParameterUpdate parameter (ToUInt32 value) world
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> OnI16ParameterUpdate parameter (ToInt16 value) world
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> OnU16ParameterUpdate parameter (ToUInt16 value) world
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> OnI32ParameterUpdate parameter (ToInt32 value) world
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> On64ParameterUpdate parameter (ToInt64 value) world
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> OnPairParameterUpdate parameter (ToInt16 value.[2..], ToInt16 value.[6..])  world
    
    | Parameter.Karma -> world
    
    | _ -> world
    
let InitialCharacterStatus (info: CharacterStatusRaw) (world: World) =
    let primary = {
        STR = int16 info.STR; AGI = int16 info.STR; DEX = int16 info.DEX
        VIT = int16 info.VIT; LUK = int16 info.LUK; INT = int16 info.INT
    }
    let upgrade = {
        STR = int16 info.USTR; AGI = int16 info.USTR; DEX = int16 info.UDEX
        VIT = int16 info.UVIT; LUK = int16 info.ULUK; INT = int16 info.UINT
    }
    
    let battle = {world.Player.BattleParameters with
                      Attack1 = info.ATK;Attack2 = info.ATK2;
                      MagicAttack1 = info.MATK_MIN;MagicAttack2 = info.MATK_MAX
                      Defense1 = info.DEF;Defense2 = info.DEF2
                      MagicDefense1 = info.MDEF;MagicDefense2 = info.MDEF2;Hit = info.HIT
                      Flee1 = info.FLEE; Flee2 = info.FLEE;Critical = info.CRIT;}
    let attributes = {world.Player.Attributes
                        with Primary = primary; UpgradeCost = upgrade}
    
    setl World._Player
        {world.Player with
             Attributes = attributes
             AttributePoints = info.Points
             BattleParameters = battle
         }
    <| world
    
let UpdatePartyMemberHP id hp maxHp world =
    match World.Unit world id with
    | None -> Logger.Warn ("Unit {aid}: Could not find party member to update HP.", id); world
    | Some unit -> World.UpdateUnit {unit with HP=hp; MaxHP=maxHp} world