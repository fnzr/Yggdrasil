module Yggdrasil.IO.Incoming

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open Microsoft.FSharp.Reflection
open NLog
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

let OnU32ParameterUpdate code value (state: State) =
    let p = match code with
            | Parameter.Weight -> { state.Inventory with Weight = value }
            | Parameter.MaxWeight -> { state.Inventory with MaxWeight = value }
            | _ -> state.Inventory
    if p <> state.Inventory then
        state.Inventory <- p
        state.PostInventory()
    else
        let p = match code with
                | Parameter.SkillPoints -> { state.Level with SkillPoints = value }
                | Parameter.JobLevel -> { state.Level with JobLevel = value }
                | Parameter.BaseLevel -> { state.Level with BaseLevel = value }
                | _ -> state.Level
        if p <> state.Level then
                state.Level <- p
                state.PostLevel()
        else
            let p = match code with
                    | Parameter.MaxHP -> { state.HPSP with MaxHP = value }
                    | Parameter.MaxSP -> { state.HPSP with MaxSP = value }
                    | Parameter.SP -> { state.HPSP with SP = value }
                    | Parameter.HP -> { state.HPSP with HP = value }
                    | _ -> state.HPSP
            if p <> state.HPSP then
                state.HPSP <- p
                state.PostHPSP()
    
let OnI16ParameterUpdate code value (state: State) =
    let p = match code with
    //| Parameter.Manner -> { agent.Parameters with Manner = value }
            | Parameter.Hit -> { state.BattleParameters with Hit = value }
            | Parameter.Flee1 -> { state.BattleParameters with Flee1 = value }
            | Parameter.Flee2 -> { state.BattleParameters with Flee2 = value }
            | Parameter.Critical -> { state.BattleParameters with Critical = value }
            | _ -> state.BattleParameters
    state.BattleParameters <- p
    
let OnU16ParameterUpdate code value (state: State) =
    let p = match code with    
            | Parameter.AttackSpeed -> { state.BattleParameters with AttackSpeed = value }
            | Parameter.Attack1 -> { state.BattleParameters with Attack1 = value }
            | Parameter.Attack2 -> { state.BattleParameters with Attack2 = value }
            | Parameter.Defense1 -> { state.BattleParameters with Defense1 = value }
            | Parameter.Defense2 -> { state.BattleParameters with Defense2 = value }
            | Parameter.MagicAttack1 -> { state.BattleParameters with MagicAttack1 = value }
            | Parameter.MagicAttack2 -> { state.BattleParameters with MagicAttack2 = value }
            | Parameter.MagicDefense1 -> { state.BattleParameters with MagicDefense1 = value }
            | Parameter.MagicDefense2 -> { state.BattleParameters with MagicDefense2 = value }
            | Parameter.AttackRange -> { state.BattleParameters with AttackRange = value }
            | Parameter.Speed -> { state.BattleParameters with Speed = Convert.ToInt64(value) }
            | _ -> state.BattleParameters
    state.BattleParameters <- p
    
let OnI32ParameterUpdate code value (agent: State) =
    if code = Parameter.Zeny then
        agent.Inventory <- {agent.Inventory with Zeny = value}
        agent.PostInventory()
    else
        let p = match code with
                | Parameter.USTR -> { agent.BattleParameters with STRUpgradeCost = value }
                | Parameter.UAGI -> { agent.BattleParameters with AGIUpgradeCost = value }
                | Parameter.UDEX -> { agent.BattleParameters with DEXUpgradeCost = value }
                | Parameter.UVIT -> { agent.BattleParameters with VITUpgradeCost = value }
                | Parameter.ULUK -> { agent.BattleParameters with LUKUpgradeCost = value }
                | Parameter.UINT -> { agent.BattleParameters with INTUpgradeCost = value }
                | _ -> agent.BattleParameters
        if p <> agent.BattleParameters then
            agent.BattleParameters <- p
            agent.PostBattleParameters()
        

let On64ParameterUpdate code value state =
    let p = match code with
            | Parameter.BaseExp -> { state.Level with BaseExp = value }
            | Parameter.JobExp -> { state.Level with JobExp = value }
            | Parameter.NextBaseExp -> { state.Level with NextBaseExp = value }
            | Parameter.NextJobExp -> { state.Level with NextJobExp = value }
            | _ -> state.Level
    state.Level <- p
    
let OnPairParameterUpdate code value (state: State) =
    let p = match code with
            | Parameter.STR -> { state.BattleParameters with STRRaw = value }
            | Parameter.AGI -> { state.BattleParameters with AGIRaw = value }
            | Parameter.DEX -> { state.BattleParameters with DEXRaw = value }
            | Parameter.VIT -> { state.BattleParameters with VITRaw = value }
            | Parameter.LUK -> { state.BattleParameters with LUKRaw = value }
            | Parameter.INT -> { state.BattleParameters with INTRaw = value }
            | _ -> state.BattleParameters
    state.BattleParameters <- p

let OnParameterChange state parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> OnU32ParameterUpdate parameter (ToUInt32 value) state
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> OnI16ParameterUpdate parameter (ToInt16 value) state
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> OnU16ParameterUpdate parameter (ToUInt16 value) state
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> OnI32ParameterUpdate parameter (ToInt32 value) state
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> On64ParameterUpdate parameter (ToInt64 value) state
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> OnPairParameterUpdate parameter (ToUInt16 value.[2..], ToInt16 value.[6..]) state
    
    | Parameter.Karma -> ()
    
    | _ -> () //Logger.Error("Unhandled parameter: {paramCode}", parameter)

let OnNonPlayerSpawn agent data = ()//publish <| NonPlayerSpawn (MakeRecord<Unit> data [|24|])
let OnPlayerSpawn agent data =()// publish <| PlayerSpawn (MakeRecord<Unit> data [|24|])

let AddSkill (state: State) data =
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            //TODO SkillRaw -> Skill
            state.PostNewSkill <| MakeRecord<Skill> data [|24|]
            ParseSkills bytes.[37..]
    ParseSkills data

let WalkDataLock = obj()
let rec TryTakeStep (cancelToken: CancellationToken) delay (state: State) (path: (int * int) list) = async {
    do! Async.Sleep delay
    lock WalkDataLock
        (fun () ->
        if cancelToken.IsCancellationRequested then ()
        else
            state.PostPosition (fst path.Head, snd path.Head)
            match path.Tail.Length with
            | 0 ->
                state.PostDestination None
                state.WalkCancellationToken <- None
            | _ ->
                let speed = Convert.ToInt32 state.BattleParameters.Speed
                Async.StartImmediate <| TryTakeStep cancelToken speed state path.Tail
            )
}

let OnAgentStartedWalking state (data: byte[]) =
    let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[4..]
    lock WalkDataLock
         (fun () ->
            match state.WalkCancellationToken with
            | Some (token) -> token.Cancel()
            | None -> ()
            state.PostDestination None
            
            let destination = (Convert.ToInt32 x1, Convert.ToInt32 y1)            
            let path = Pathfinding.AStar (Maps.GetMapData (state.MapName))
                                          (Convert.ToInt32 x0, Convert.ToInt32 y0) destination
            if path.Length > 0 then
                state.PostDestination <| Some(destination)
                let delay = Convert.ToInt64 (ToUInt32 data) - state.TickOffset// - agent.Parameters.Speed
                let tokenSource = new CancellationTokenSource()
                state.WalkCancellationToken <- Some(tokenSource)
                Async.Start (TryTakeStep tokenSource.Token (Convert.ToInt32 delay) state path)
         )
let OnConnectionAccepted state (data: byte[]) =
    let (x, y, _) = UnpackPosition data.[4..]
    state.TickOffset <- Convert.ToInt64 (ToUInt32 data.[0..]) - Handshake.GetCurrentTick()
    state.PostPosition (Convert.ToInt32 x, Convert.ToInt32 y)
    state.BehaviorMailbox.Post ConnectionAccepted
    //Logger.Info("Starting position: {pos}", agent.Position)
    state.Dispatch Command.DoneLoadingMap
    
let OnWeightSoftCap state (data: byte[]) =
    state.Inventory <- {state.Inventory with WeightSoftCap = ToInt32 data}
    state.PostInventory()

let OnPacketReceived state packetType (data: byte[]) =
    match packetType with
        | 0x13aus -> OnParameterChange state Parameter.AttackRange data.[2..]
        | 0x00b0us -> OnParameterChange state (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange state (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange state (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> OnWeightSoftCap state data.[2..]         
        | 0x9ffus -> OnNonPlayerSpawn state data.[4..]
        | 0x9feus -> OnPlayerSpawn state data.[4..]
        | 0x10fus -> AddSkill state data.[4..]
        | 0x0087us -> OnAgentStartedWalking state data.[2..]
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> ()        
        | 0x0bdus (* ZC_STATUS *) -> ()
        | 0x0086us (* ZC_NOTIFY_PLAYERMOVE *) -> ()
        | 0x2ebus -> OnConnectionAccepted state data.[2..]
        | 0x121us (* cart info *) -> ()
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> ()
        | 0x007fus -> state.TickOffset <- Convert.ToInt64(ToUInt32 data.[2..]) - Handshake.GetCurrentTick()
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *) -> ()
        | 0x0081us -> ()//Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> () //Logger.Error("Unhandled packet {packetType:X} with length {length}", unknown, data.Length) //shutdown()
