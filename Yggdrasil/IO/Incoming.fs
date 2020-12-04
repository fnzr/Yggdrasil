module Yggdrasil.IO.Incoming

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.Messages
open Yggdrasil.Types
open Yggdrasil.Utils

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
     8uy//(data.[5] <<< 8)  //this doesnt work //dirY
    )

let Logger = LogManager.GetCurrentClassLogger()

let OnParameterChange (publish: Report -> unit) parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> publish <| StatusU32 (parameter, ToUInt32 value)
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> publish <| StatusI16 (parameter, ToInt16 value)
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> publish <| StatusU16 (parameter, ToUInt16 value)
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> publish <| StatusI32 (parameter, ToInt32 value)
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> publish <| Status64 (parameter, ToInt64 value)
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> publish <| StatusPair (parameter, ToUInt16 value.[2..], ToInt16 value.[6..])
    
    | Parameter.Karma -> ()
    
    | _ -> Logger.Error("Unhandled parameter: {paramCode}", parameter)
    
let OnWeightSoftCap publish value = value |> ToInt32 |> WeightSoftCap |> publish

let OnConnectionAccepted publish (value: byte[]) =
    let (x, y, dir) = UnpackPosition value.[4..]
    publish <| ConnectionAccepted {
        StartTime = ToUInt32 value.[0..]
        X = x
        Y = y
        Direction = dir
    }
    
let OnSelfStartWalking publish (data: byte[]) =
    //TODO
    let (x0, y0, x1, y1, sx, sy) = UnpackPosition2 data.[4..]
    ()
    
let OnNonPlayerSpawn publish data = publish <| NonPlayerSpawn (MakeRecord<Unit> data [|24|])
let OnPlayerSpawn publish data = publish <| PlayerSpawn (MakeRecord<Unit> data [|24|])
let OnServerTime publish data = publish <| ServerTime (ToUInt32 data)

let AddSkill publish data =
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            //TODO SkillRaw -> Skill
            publish <| AddSkill (MakeRecord<Skill> data [|24|])
            ParseSkills bytes.[37..]
    ParseSkills data    
   
let ZonePacketHandler (publish: Report -> unit) =
    let rec handler (packetType: uint16) (data: byte[]) =
        //Logger.Debug ("Received packet: {packet:X}", packetType)
        match packetType with
        | 0x13aus -> OnParameterChange publish Parameter.AttackRange data.[2..]
        | 0x00b0us -> OnParameterChange publish (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange publish (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange publish (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> OnWeightSoftCap publish data.[2..]        
        | 0x9ffus -> OnNonPlayerSpawn publish data.[4..]
        | 0x9feus -> OnPlayerSpawn publish data.[4..]
        | 0x10fus -> AddSkill publish data.[4..]
        | 0x0087us -> OnSelfStartWalking publish data.[2..]
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> ()        
        | 0x0bdus (* ZC_STATUS *) -> ()
        | 0x0086us (* ZC_NOTIFY_PLAYERMOVE *) -> ()
        | 0x2ebus -> OnConnectionAccepted publish data.[2..]
        | 0x121us (* cart info *) -> ()
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> ()
        | 0x007fus -> OnServerTime publish data.[2..]
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *) -> ()
        | 0x0081us -> Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> () //Logger.Error("Unhandled packet {packetType:X} with length {length}", unknown, data.Length) //shutdown()
    handler
