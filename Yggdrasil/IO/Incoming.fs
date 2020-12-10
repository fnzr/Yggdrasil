module Yggdrasil.IO.Incoming

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil
open Yggdrasil.Navigation
open Yggdrasil.Types
open Yggdrasil.Utils
let Logger = LogManager.GetCurrentClassLogger()

let MakeRecord<'T> (data: byte[]) (stringSizes: int[]) =
    let queue = Queue<obj>()
    let fields = typeof<'T>.GetProperties()
    let rec loop (properties: PropertyInfo[]) (data: byte[]) (stringSizes: int[]) =
        match properties with
        | [||] -> FSharpValue.MakeRecord(typeof<'T>, queue.ToArray()) :?> 'T
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

let OnU32ParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.Weight -> parameters.Weight <- value
    | Parameter.MaxWeight -> parameters.MaxWeight <- value
    | Parameter.SkillPoints -> parameters.SkillPoints <- value
    | Parameter.JobLevel -> parameters.JobLevel <- value
    | Parameter.BaseLevel -> parameters.BaseLevel <- value
    | Parameter.MaxHP -> parameters.MaxHP <- value
    | Parameter.MaxSP -> parameters.MaxSP <- value
    | Parameter.SP -> parameters.SP <- value
    | Parameter.HP -> parameters.HP <- value
    | _ -> ()
    
let OnI16ParameterUpdate code value (parameters: Parameters) =
    match code with
    //| Parameter.Manner -> parameters.Manner <- value
    | Parameter.Hit -> parameters.Hit <- value
    | Parameter.Flee1 -> parameters.Flee1 <- value
    | Parameter.Flee2 -> parameters.Flee2 <- value
    | Parameter.Critical -> parameters.Critical <- value
    | _ -> ()
    
let OnU16ParameterUpdate code value (parameters: Parameters) =
    match code with    
    | Parameter.AttackSpeed -> parameters.AttackSpeed <- value
    | Parameter.Attack1 -> parameters.Attack1 <- value
    | Parameter.Attack2 -> parameters.Attack2 <- value
    | Parameter.Defense1 -> parameters.Defense1 <- value
    | Parameter.Defense2 -> parameters.Defense2 <- value
    | Parameter.MagicAttack1 -> parameters.MagicAttack1 <- value
    | Parameter.MagicAttack2 -> parameters.MagicAttack2 <- value
    | Parameter.MagicDefense1 -> parameters.MagicDefense1 <- value
    | Parameter.MagicDefense2 -> parameters.MagicDefense2 <- value
    | Parameter.AttackRange -> parameters.AttackRange <- value
    | Parameter.Speed -> parameters.Speed <- Convert.ToInt64(value)
    | _ -> ()
    
let OnI32ParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.Zeny -> parameters.Zeny <- value
    | Parameter.USTR -> parameters.STRUpgradeCost <- value
    | Parameter.UAGI -> parameters.AGIUpgradeCost <- value
    | Parameter.UDEX -> parameters.DEXUpgradeCost <- value
    | Parameter.UVIT -> parameters.VITUpgradeCost <- value
    | Parameter.ULUK -> parameters.LUKUpgradeCost <- value
    | Parameter.UINT -> parameters.INTUpgradeCost <- value
    | _ -> ()

let On64ParameterUpdate code value parameters =
    match code with
    | Parameter.BaseExp -> parameters.BaseExp <- value
    | Parameter.JobExp -> parameters.JobExp <- value
    | Parameter.NextBaseExp -> parameters.NextBaseExp <- value
    | Parameter.NextJobExp -> parameters.NextJobExp <- value
    | _ -> ()
    
let OnPairParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.STR -> parameters.STRRaw <- value
    | Parameter.AGI -> parameters.AGIRaw <- value
    | Parameter.DEX -> parameters.DEXRaw <- value
    | Parameter.VIT -> parameters.VITRaw <- value
    | Parameter.LUK -> parameters.LUKRaw <- value
    | Parameter.INT -> parameters.INTRaw <- value
    | _ -> ()

let OnParameterChange agent parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> OnU32ParameterUpdate parameter (ToUInt32 value) agent.Parameters
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> OnI16ParameterUpdate parameter (ToInt16 value) agent.Parameters
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> OnU16ParameterUpdate parameter (ToUInt16 value) agent.Parameters
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> OnI32ParameterUpdate parameter (ToInt32 value) agent.Parameters
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> On64ParameterUpdate parameter (ToInt64 value) agent.Parameters
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> OnPairParameterUpdate parameter (ToUInt16 value.[2..], ToInt16 value.[6..]) agent.Parameters
    
    | Parameter.Karma -> ()
    
    | _ -> () //Logger.Error("Unhandled parameter: {paramCode}", parameter)

let OnNonPlayerSpawn agent data = ()//publish <| NonPlayerSpawn (MakeRecord<Unit> data [|24|])
let OnPlayerSpawn agent data =()// publish <| PlayerSpawn (MakeRecord<Unit> data [|24|])

let AddSkill agent data =
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            //TODO SkillRaw -> Skill
            agent.Skills <- MakeRecord<Skill> data [|24|] :: agent.Skills
            ParseSkills bytes.[37..]
    ParseSkills data

let WalkDataLock = obj()
let rec TryTakeStep delay agent destination (path: (int * int) list) = async {
    Async.Sleep delay |> ignore
    lock WalkDataLock
        (fun () ->
        if destination = agent.Destination then
            agent.Position <- (fst path.Head, snd path.Head)
            match List.tail path with
            | [] -> agent.Destination <- None
            | p ->                    
                Async.Start (async {
                    let speed = Convert.ToInt32 agent.Parameters.Speed
                    return! TryTakeStep speed agent destination p
                })
        )
}

let OnAgentStartedWalking agent (data: byte[]) =
    let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[4..]
    lock WalkDataLock
         (fun () ->
            let destination = (Convert.ToInt32 x1, Convert.ToInt32 y1)
            agent.Destination <- Some(destination)
            let path = Pathfinding.AStar (Maps.GetMapData (agent.MapName))
                                          (Convert.ToInt32 x0, Convert.ToInt32 y0) destination
            let delay = Convert.ToInt64 (ToUInt32 data) - agent.TickOffset// - agent.Parameters.Speed            
            Async.Start (TryTakeStep (Convert.ToInt32 delay) agent agent.Destination path) |> ignore
         )
let OnConnectionAccepted agent (data: byte[]) =
    let (x, y, _) = UnpackPosition data.[4..]
    agent.TickOffset <- Convert.ToInt64 (ToUInt32 data.[0..]) - Scheduling.GetCurrentTick()
    agent.Position <- (Convert.ToInt32 x, Convert.ToInt32 y)
    Logger.Info("Starting position: {pos}", agent.Position)
    agent.Dispatch Command.DoneLoadingMap

let OnPacketReceived agent packetType (data: byte[]) =
    match packetType with
        | 0x13aus -> OnParameterChange agent Parameter.AttackRange data.[2..]
        | 0x00b0us -> OnParameterChange agent (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange agent (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange agent (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> agent.WeightSoftCap <- ToInt32 data.[2..]         
        | 0x9ffus -> OnNonPlayerSpawn agent data.[4..]
        | 0x9feus -> OnPlayerSpawn agent data.[4..]
        | 0x10fus -> AddSkill agent data.[4..]
        | 0x0087us -> OnAgentStartedWalking agent data.[2..]
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> ()        
        | 0x0bdus (* ZC_STATUS *) -> ()
        | 0x0086us (* ZC_NOTIFY_PLAYERMOVE *) -> ()
        | 0x2ebus -> OnConnectionAccepted agent data.[2..]
        | 0x121us (* cart info *) -> ()
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> ()
        | 0x007fus -> agent.TickOffset <- Convert.ToInt64(ToUInt32 data.[2..]) - Scheduling.GetCurrentTick()
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *) -> ()
        | 0x0081us -> ()//Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> () //Logger.Error("Unhandled packet {packetType:X} with length {length}", unknown, data.Length) //shutdown()
