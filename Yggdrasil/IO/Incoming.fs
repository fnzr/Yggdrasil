module Yggdrasil.IO.Incoming

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
open Yggdrasil.Game.Event
let Logger = LogManager.GetLogger("Incoming")

let MakePartialRecord<'T> (data: byte[]) (stringSizes: int[]) =
    let queue = Queue<obj>()
    let fields = typeof<'T>.GetProperties()
    let rec loop (properties: PropertyInfo[]) (data: byte[]) (stringSizes: int[]) =
        match properties with
        | [||] -> FSharpValue.MakeRecord(typeof<'T>, queue.ToArray()) :?> 'T, data
        | _ ->
            let property = properties.[0]
            let size, stringsS = if property.PropertyType = typeof<string>
                                    then stringSizes.[0], stringSizes.[1..]
                                    else Marshal.SizeOf(property.PropertyType), stringSizes
            
            let value = if property.PropertyType = typeof<int32> then ToInt32 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint32> then ToUInt32 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<byte> then data.[0] :> obj
                        elif property.PropertyType = typeof<sbyte> then Convert.ToSByte data.[0] :> obj
                        elif property.PropertyType = typeof<int16> then ToInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint16> then ToUInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<string> then ToString data.[..size-1] :> obj
                        else raise (ArgumentException "Unhandled type")
            queue.Enqueue(value);
            loop properties.[1..] data.[size..] stringsS    
    loop fields data stringSizes
    
let MakeRecord<'T> data = fst <| MakePartialRecord<'T> data [||]
let UnpackPosition (data: byte[]) =
    ((data.[0] <<< 2) ||| (data.[1] >>> 6),  //X
     (data.[1] <<< 4) ||| (data.[2] >>> 4),  //Y
     data.[2] <<< 4 //not sure about this //Direction
    )
    
let UnpackPosition2 (data: byte[]) =
    (int16 (data.[0] <<< 2)) ||| int16 (data.[1]&&&(~~~0x3fuy) >>> 6), //x0
    ((int16 (data.[1]&&&0x3fuy)) <<<4) ||| (int16 (data.[2]>>>4)), //y0
    (int16 (data.[2]&&&(0x0fuy)) <<< 6) ||| (int16 (data.[3]&&&(~~~0x03uy)) >>> 2), //x1
    (int16 (data.[3]&&&0x3uy) <<< 8) ||| int16 data.[4], // y1   
    (data.[5] >>> 4),  //dirX
    (data.[5] <<< 4) //dirY

let OnU32ParameterUpdate code value (player: Player) =
    match code with
        | Parameter.Weight -> player.Inventory.Weight <- value
        | Parameter.MaxWeight -> player.Inventory.MaxWeight <- value
    
        | Parameter.SkillPoints -> player.Level.SkillPoints <- value
        | Parameter.JobLevel -> player.Level.JobLevel <- value
        | Parameter.BaseLevel -> player.Level.BaseLevel <- value
    
        | Parameter.MaxHP -> player.Unit.MaxHP <- int value
        | Parameter.MaxSP -> player.Health.MaxSP <- value
        | Parameter.SP -> player.Health.SP <- value
        | Parameter.HP -> player.Unit.HP <- int value
        | _ -> ()
    
let OnI16ParameterUpdate code value (player: Player) =
    match code with
    //| Parameter.Manner -> agent.Parameters.Manner <- value
        | Parameter.Hit -> player.BattleParameters.Hit <- value
        | Parameter.Flee1 -> player.BattleParameters.Flee1 <- value
        | Parameter.Flee2 -> player.BattleParameters.Flee2 <- value
        | Parameter.Critical -> player.BattleParameters.Critical <- value
        | _ -> ()
    
let OnU16ParameterUpdate code value (player: Player) =
    match code with    
        | Parameter.AttackSpeed -> player.BattleParameters.AttackSpeed <- value
        | Parameter.Attack1 -> player.BattleParameters.Attack1 <- value
        | Parameter.Attack2 -> player.BattleParameters.Attack2 <- value
        | Parameter.Defense1 -> player.BattleParameters.Defense1 <- value
        | Parameter.Defense2 -> player.BattleParameters.Defense2 <- value
        | Parameter.MagicAttack1 -> player.BattleParameters.MagicAttack1 <- value
        | Parameter.MagicAttack2 -> player.BattleParameters.MagicAttack2 <- value
        | Parameter.MagicDefense1 -> player.BattleParameters.MagicDefense1 <- value
        | Parameter.MagicDefense2 -> player.BattleParameters.MagicDefense2 <- value
        | Parameter.AttackRange -> player.BattleParameters.AttackRange <- value
        | Parameter.Speed -> player.Unit.Speed <- int16 value
        | _ -> ()
    
let OnI32ParameterUpdate code value (player: Player) =
    match code with
        | Parameter.Zeny -> player.Inventory.Zeny <- value
        | Parameter.USTR -> player.BattleParameters.STRUpgradeCost <- value
        | Parameter.UAGI -> player.BattleParameters.AGIUpgradeCost <- value
        | Parameter.UDEX -> player.BattleParameters.DEXUpgradeCost <- value
        | Parameter.UVIT -> player.BattleParameters.VITUpgradeCost <- value
        | Parameter.ULUK -> player.BattleParameters.LUKUpgradeCost <- value
        | Parameter.UINT -> player.BattleParameters.INTUpgradeCost <- value
        | _ -> ()
        

let On64ParameterUpdate code value (player: Player) =
    match code with
        | Parameter.BaseExp -> player.Level.BaseExp <- value
        | Parameter.JobExp -> player.Level.JobExp <- value
        | Parameter.NextBaseExp -> player.Level.NextBaseExp <- value
        | Parameter.NextJobExp -> player.Level.NextJobExp <- value
        | _ -> ()
    
let OnPairParameterUpdate code value (player: Player) =
    match code with
        | Parameter.STR -> player.BattleParameters.STRRaw <- value
        | Parameter.AGI -> player.BattleParameters.AGIRaw <- value
        | Parameter.DEX -> player.BattleParameters.DEXRaw <- value
        | Parameter.VIT -> player.BattleParameters.VITRaw <- value
        | Parameter.LUK -> player.BattleParameters.LUKRaw <- value
        | Parameter.INT -> player.BattleParameters.INTRaw <- value
        | _ -> ()

let OnParameterChange (player: Player) parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> OnU32ParameterUpdate parameter (ToUInt32 value) player
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> OnI16ParameterUpdate parameter (ToInt16 value) player
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> OnU16ParameterUpdate parameter (ToUInt16 value) player
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> OnI32ParameterUpdate parameter (ToInt32 value) player
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> On64ParameterUpdate parameter (ToInt64 value) player
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> OnPairParameterUpdate parameter (ToUInt16 value.[2..], ToInt16 value.[6..]) player
    
    | Parameter.Karma -> ()
    
    | _ -> ()

let OnUnitSpawn (world: World) data =
    let (part1, leftover) = MakePartialRecord<UnitRawPart1> data [||]    
    let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
    world.SpawnUnit <| CreateNonPlayer part1 part2 world.Inbox
    
let OnNonPlayerSpawn = OnUnitSpawn
let OnPlayerSpawn = OnUnitSpawn

let OnWalkingUnitSpawn (world: World) data =
    let (part1, leftover) = MakePartialRecord<UnitRawPart1> data [||]
    //skip MoveStartTime: uint32 
    let (part2, _) = MakePartialRecord<UnitRawPart2> (leftover.[4..]) [|24|]
    world.SpawnUnit <| CreateNonPlayer part1 part2 world.Inbox

let OnUnitDisappear (world: World) data =
    world.DespawnUnit <| ToUInt32 data
        <| (Enum.Parse(typeof<DisappearReason>, string data.[4]) :?> DisappearReason)

let AddSkill (player: Player) data =
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            //TODO SkillRaw -> Skill
            let (skill, _) = MakePartialRecord<Skill> data [|24|]
            player.Skills <- skill :: player.Skills
            ParseSkills bytes.[37..]
    ParseSkills data
let OnPlayerStartedWalking (conn: Connection) (world: World) (data: byte[]) =
    let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[4..]
    let delay = Convert.ToInt64 (ToUInt32 data) - Connection.Tick - conn.TickOffset
    world.Player.Position <- (int x0, int y0)
    world.Player.Unit.Walk world.Map (int x1, int y1) delay
    
let OnConnectionAccepted (conn: Connection) (player: Player) (data: byte[]) =
    let (x, y, _) = UnpackPosition data.[4..]
    conn.TickOffset <- Convert.ToInt64 (ToUInt32 data.[0..]) - Connection.Tick
    player.Position <- (Convert.ToInt32 x, Convert.ToInt32 y)
    conn.Status <- Active
    
let OnWeightSoftCap (player: Player) (data: byte[]) = player.Inventory.WeightSoftCap <- ToInt32 data

let OnSkillCast (world: World) data =
    let castRaw = MakeRecord<RawSkillCast> data    
    match (world.GetUnit castRaw.source, world.GetUnit castRaw.target) with
    | (None, _) | (_, None) -> Logger.Warn "Missing skill cast units!"
    | (Some caster, Some target) ->
        let cast: Skill.SkillCast = {
            SkillId = castRaw.skillId
            Delay = int castRaw.delay
            Property = castRaw.property
        }
        caster.StartCast cast target

let OnMapChange (world: World) (data: byte[]) =
    world.Map <- (
        let gatFile = ToString data.[..15]
        gatFile.Substring(0, gatFile.Length - 4))
    world.Player.Position <- (data.[16..] |> ToUInt16 |> Convert.ToInt32,
                                data.[18..] |> ToUInt16 |> Convert.ToInt32)
    world.Player.Dispatch DoneLoadingMap
    
let MoveUnit (world: World) data =
    let move = MakeRecord<UnitMove> data
    let destination = (int move.X, int move.Y)
    match world.GetUnit move.aid with
    | Some unit -> unit.Walk world.Map destination 0L
    | None -> Logger.Warn ("Unhandled movement for {aid}", move.aid)
        
        
let UpdateMonsterHP (world: World) data =
    let info= MakeRecord<MonsterHPInfo> data
    match world.GetUnit info.aid with
    | Some unit ->
        unit.HP <- info.HP
        unit.MaxHP <- info.MaxHP
    | None -> Logger.Warn ("Unhandled HP update for {aid}", info.aid)
    
let DamageDealt (world: World) data =
    let info = MakeRecord<DamageInfo> data
    match world.GetUnit info.Source, world.GetUnit info.Target with
    | None, _ | _, None -> Logger.Error "Failed loading units to apply damage"
    | Some source, Some target ->
        if info.IsSPDamage > 0uy then
            Logger.Warn "IsSPDamage. I dont know what that is"
        else
            let mutable delay = int <| (int64 info.Tick) - Connection.Tick
            if delay < 0 then delay <- 0
            Async.Start <| async {
                //TODO
                do! Async.Sleep delay
                //source.
            }
        
let OnPacketReceived (game: Game) (packetType: uint16) (data: byte[]) =
    let conn = game.Connection
    let world = game.World
    let player = world.Player
    Logger.Trace("Packet: {packetType:X}", packetType)
    match packetType with
        | 0x13aus -> OnParameterChange player Parameter.AttackRange data.[2..]
        | 0x00b0us -> OnParameterChange player (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange player (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange player (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> OnWeightSoftCap player data.[2..]         
        | 0x9ffus -> OnNonPlayerSpawn world data.[4..]
        | 0x9feus -> OnPlayerSpawn world data.[4..]
        | 0x9fdus -> OnWalkingUnitSpawn world data.[4..]
        | 0x0080us -> OnUnitDisappear world data.[2..]
        | 0x10fus -> AddSkill player data.[4..]
        | 0x0087us -> OnPlayerStartedWalking conn world data.[2..]
        | 0x07fbus -> OnSkillCast world data.[2..]
        | 0x0088us -> MoveUnit world data.[2..]
        | 0x0977us -> UpdateMonsterHP world data.[2..]
        | 0x08c8us -> DamageDealt world data.[2..] 
        | 0x0adfus (* ZC_REQNAME_TITLE *) -> ()
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> ()        
        | 0x0bdus (* ZC_STATUS *) -> ()
        | 0x0086us (* ZC_NOTIFY_PLAYERMOVE *) -> ()
        | 0x2ebus -> OnConnectionAccepted conn player data.[2..]
        | 0x121us (* cart info *) -> ()
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us -> OnMapChange world data.[2..]
        | 0x007fus -> conn.TickOffset <- Convert.ToInt64(ToUInt32 data.[2..]) - Connection.Tick
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x0a30us (* ZC_ACK_REQNAMEALL2 *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *) -> ()
        | 0x0081us -> ()//Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length) //shutdown()
