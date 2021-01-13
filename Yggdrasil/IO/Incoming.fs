module Yggdrasil.IO.Incoming

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Agent.Agent
open Yggdrasil.Agent.Location
open Yggdrasil.Navigation
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Agent.Event
open Yggdrasil.Agent.Unit
let Logger = LogManager.GetLogger("Incoming")

let MakeRecord<'T> (data: byte[]) (stringSizes: int[]) =
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
    
let UnpackPosition (data: byte[]) =
    ((data.[0] <<< 2) ||| (data.[1] >>> 6),  //X
     (data.[1] <<< 4) ||| (data.[2] >>> 4),  //Y
     data.[2] <<< 4 //not sure about this //Direction
    )
    
let UnpackPosition2 (data: byte[]) =
    ((data.[0] <<< 2) ||| (data.[1] >>> 6),  //X0
     (data.[1] <<< 4) ||| (data.[2] >>> 4),  //Y0
     (data.[2] <<< 6) ||| (data.[3] >>> 2),  //X1
     (data.[3] <<< 5) ||| data.[4],  //Y1
     (data.[5] >>> 4),  //dirX
     (data.[5] &&& 0x3uy)//(data.[5] <<< 8)  //this doesnt work //dirY
    )

let OnU32ParameterUpdate code value (agent: Agent) =
    match code with
        | Parameter.Weight -> agent.Inventory.Weight <- value
        | Parameter.MaxWeight -> agent.Inventory.MaxWeight <- value
    
        | Parameter.SkillPoints -> agent.Level.SkillPoints <- value
        | Parameter.JobLevel -> agent.Level.JobLevel <- value
        | Parameter.BaseLevel -> agent.Level.BaseLevel <- value
    
        | Parameter.MaxHP -> agent.Health.MaxHP <- value
        | Parameter.MaxSP -> agent.Health.MaxSP <- value
        | Parameter.SP -> agent.Health.SP <- value
        | Parameter.HP -> agent.Health.HP <- value
        | _ -> ()
    
let OnI16ParameterUpdate code value (agent: Agent) =
    match code with
    //| Parameter.Manner -> agent.Parameters.Manner <- value
        | Parameter.Hit -> agent.BattleParameters.Hit <- value
        | Parameter.Flee1 -> agent.BattleParameters.Flee1 <- value
        | Parameter.Flee2 -> agent.BattleParameters.Flee2 <- value
        | Parameter.Critical -> agent.BattleParameters.Critical <- value
        | _ -> ()
    
let OnU16ParameterUpdate code value (agent: Agent) =
    match code with    
        | Parameter.AttackSpeed -> agent.BattleParameters.AttackSpeed <- value
        | Parameter.Attack1 -> agent.BattleParameters.Attack1 <- value
        | Parameter.Attack2 -> agent.BattleParameters.Attack2 <- value
        | Parameter.Defense1 -> agent.BattleParameters.Defense1 <- value
        | Parameter.Defense2 -> agent.BattleParameters.Defense2 <- value
        | Parameter.MagicAttack1 -> agent.BattleParameters.MagicAttack1 <- value
        | Parameter.MagicAttack2 -> agent.BattleParameters.MagicAttack2 <- value
        | Parameter.MagicDefense1 -> agent.BattleParameters.MagicDefense1 <- value
        | Parameter.MagicDefense2 -> agent.BattleParameters.MagicDefense2 <- value
        | Parameter.AttackRange -> agent.BattleParameters.AttackRange <- value
        | Parameter.Speed -> agent.BattleParameters.Speed <- Convert.ToInt64(value)
        | _ -> ()
    
let OnI32ParameterUpdate code value (agent: Agent) =
    match code with
        | Parameter.Zeny -> agent.Inventory.Zeny <- value
        | Parameter.USTR -> agent.BattleParameters.STRUpgradeCost <- value
        | Parameter.UAGI -> agent.BattleParameters.AGIUpgradeCost <- value
        | Parameter.UDEX -> agent.BattleParameters.DEXUpgradeCost <- value
        | Parameter.UVIT -> agent.BattleParameters.VITUpgradeCost <- value
        | Parameter.ULUK -> agent.BattleParameters.LUKUpgradeCost <- value
        | Parameter.UINT -> agent.BattleParameters.INTUpgradeCost <- value
        | _ -> ()
        

let On64ParameterUpdate code value (agent: Agent) =
    match code with
        | Parameter.BaseExp -> agent.Level.BaseExp <- value
        | Parameter.JobExp -> agent.Level.JobExp <- value
        | Parameter.NextBaseExp -> agent.Level.NextBaseExp <- value
        | Parameter.NextJobExp -> agent.Level.NextJobExp <- value
        | _ -> ()
    
let OnPairParameterUpdate code value (agent: Agent) =
    match code with
        | Parameter.STR -> agent.BattleParameters.STRRaw <- value
        | Parameter.AGI -> agent.BattleParameters.AGIRaw <- value
        | Parameter.DEX -> agent.BattleParameters.DEXRaw <- value
        | Parameter.VIT -> agent.BattleParameters.VITRaw <- value
        | Parameter.LUK -> agent.BattleParameters.LUKRaw <- value
        | Parameter.INT -> agent.BattleParameters.INTRaw <- value
        | _ -> ()

let OnParameterChange (agent: Agent) parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> OnU32ParameterUpdate parameter (ToUInt32 value) agent
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> OnI16ParameterUpdate parameter (ToInt16 value) agent
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> OnU16ParameterUpdate parameter (ToUInt16 value) agent
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> OnI32ParameterUpdate parameter (ToInt32 value) agent
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> On64ParameterUpdate parameter (ToInt64 value) agent
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> OnPairParameterUpdate parameter (ToUInt16 value.[2..], ToInt16 value.[6..]) agent
    
    | Parameter.Karma -> ()
    
    | _ -> ()

let OnUnitSpawn (agent: Agent) data =
    let (part1, leftover) = MakeRecord<UnitRawPart1> data [||]    
    let (part2, _) = MakeRecord<UnitRawPart2> leftover [|24|]
    agent.SpawnUnit <| CreateUnit part1 part2
    
let OnNonPlayerSpawn (agent: Agent) data =
    OnUnitSpawn agent data
let OnPlayerSpawn = OnUnitSpawn

let OnWalkingUnitSpawn (agent: Agent) data =
    let (part1, leftover) = MakeRecord<UnitRawPart1> data [||]
    //skip MoveStartTime: uint32 
    let (part2, _) = MakeRecord<UnitRawPart2> (leftover.[4..]) [|24|]
    agent.SpawnUnit <| CreateUnit part1 part2

let OnUnitDespawn (agent: Agent) data = agent.DespawnUnit <| ToUInt32 data

let AddSkill (agent: Agent) data =
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            //TODO SkillRaw -> Skill
            let (skill, _) = MakeRecord<Skill> data [|24|]
            agent.Skills <- skill :: agent.Skills
            ParseSkills bytes.[37..]
    ParseSkills data
let OnAgentStartedWalking (agent: Agent) (data: byte[]) =
    let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[4..]
    let delay = Convert.ToInt64 (ToUInt32 data) - Agent.Tick - agent.TickOffset
    StartWalk agent.Location
        (Convert.ToInt32 x0, Convert.ToInt32 y0)
        (Convert.ToInt32 x1, Convert.ToInt32 y1)
        delay (Convert.ToInt32 agent.BattleParameters.Speed)
    
let OnConnectionAccepted (agent: Agent) (data: byte[]) =
    let (x, y, _) = UnpackPosition data.[4..]
    agent.TickOffset <- Convert.ToInt64 (ToUInt32 data.[0..]) - Agent.Tick
    agent.Location.Position <- (Convert.ToInt32 x, Convert.ToInt32 y)
    agent.Publish <| Connection Active
    
let OnWeightSoftCap (agent: Agent) (data: byte[]) = agent.Inventory.WeightSoftCap <- ToInt32 data

let OnSkillCast (agent: Agent) data =
    let (cast, _) = MakeRecord<SkillCast> data [||]
    match (agent.Unit cast.source, agent.Unit cast.target) with
    | (None, _) | (_, None) -> Logger.Warn "Missing skill cast units!"
    | (Some caster, Some target) ->
        //TODO emit casting skill event
        Logger.Info ("{caster} casting skill {skill} on {target}", caster.Name, cast.skillId, target.Name)

let OnMapChange (agent: Agent) (data: byte[]) =
    agent.ChangeMap (
        let gatFile = ToString data.[..15]
        gatFile.Substring(0, gatFile.Length - 4))
    agent.Location.Position <- (data.[16..] |> ToUInt16 |> Convert.ToInt32,
                                data.[18..] |> ToUInt16 |> Convert.ToInt32)
    

let OnPacketReceived (agent: Agent) (packetType: uint16) (data: byte[]) =
    Logger.Trace("Packet: {packetType:X}", packetType)
    match packetType with
        | 0x13aus -> OnParameterChange agent Parameter.AttackRange data.[2..]
        | 0x00b0us -> OnParameterChange agent (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange agent (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange agent (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> OnWeightSoftCap agent data.[2..]         
        | 0x9ffus -> OnNonPlayerSpawn agent data.[4..]
        | 0x9feus -> OnPlayerSpawn agent data.[4..]
        | 0x9fdus -> OnWalkingUnitSpawn agent data.[4..]
        | 0x0080us -> OnUnitDespawn agent data.[2..]
        | 0x10fus -> AddSkill agent data.[4..]
        | 0x0087us -> OnAgentStartedWalking agent data.[2..]
        | 0x07fbus -> OnSkillCast agent data.[2..]
        | 0x0adfus (* ZC_REQNAME_TITLE *) -> ()
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> ()        
        | 0x0bdus (* ZC_STATUS *) -> ()
        | 0x0086us (* ZC_NOTIFY_PLAYERMOVE *) -> ()
        | 0x2ebus -> OnConnectionAccepted agent data.[2..]
        | 0x121us (* cart info *) -> ()
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us -> OnMapChange agent data.[2..]
        | 0x007fus -> agent.TickOffset <- Convert.ToInt64(ToUInt32 data.[2..]) - Agent.Tick
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *) -> ()
        | 0x0081us -> ()//Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length) //shutdown()
