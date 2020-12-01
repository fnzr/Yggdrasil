module Yggdrasil.IO.Incoming

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open Microsoft.FSharp.Reflection
open NLog
open Yggdrasil.PacketTypes
open Yggdrasil.Reporter
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
                        elif property.PropertyType = typeof<int16> then ToInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint16> then ToUInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<string> then (Encoding.UTF8.GetString data.[..size-1]) :> obj
                        else raise (ArgumentException "Unhandled type")
            queue.Enqueue(value);
            loop properties.[1..] data.[size..] stringsS    
    loop fields data stringSizes

let OnParameterChange (publish: Report -> unit) parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> publish <| AgentReport (StatusU32 (parameter, ToUInt32 value))
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> publish <| AgentReport (StatusI16 (parameter, ToInt16 value))
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> publish <| AgentReport (StatusU16 (parameter, ToUInt16 value))
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> publish <| AgentReport (StatusI32 (parameter, ToInt32 value))
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> publish <| AgentReport (Status64 (parameter, ToInt64 value))
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> publish <| AgentReport (StatusPair (parameter, ToUInt16 value.[2..], ToInt16 value.[6..]))
    
    | Parameter.Karma -> ()
    
    | _ -> Logger.Error("Unhandled parameter: {paramCode}", parameter)
    
let OnWeightSoftCap publish value = value |> ToInt32 |> WeightSoftCap |> AgentReport |> publish

let OnConnectionAccepted publish value =
    publish <| AgentReport (ConnectionAccepted(MakeRecord<StartData> value [||]))
    
let OnUnitSpawn publish data = publish <| SystemReport (UnitSpawn (MakeRecord<Unit> data [|24|]))    
   
let ZonePacketHandler (publish: Report -> unit) =
    let rec handler (packetType: uint16) (data: byte[]) =
        match packetType with
        | 0x13aus -> OnParameterChange publish Parameter.AttackRange data.[2..] 
        //| 0x121us (* cart info *) -> messenger.Post <| StatusUpdate (data.[2..] |> ToUInt16, data.[6..] |> ToInt32)
        | 0x00b0us -> OnParameterChange publish (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange publish (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange publish (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> OnWeightSoftCap publish data.[2..]
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> ()
        | 0x007fus (* ZC_NOTIFY_TIME *)  -> ()
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x9ffus -> OnUnitSpawn publish data.[4..]
        //| 0x9feus -> SpawnPlayer robot.Agent data.[4..]
        //| 0x10fus -> AddSkill robot.Agent data.[4..]
        //| 0x0087us -> StartWalk robot.Agent data.[2..]
        //| 0x080eus -> PartyMemberHPUpdate robot.Agent data.[2..]
        | 0x2ebus -> OnConnectionAccepted publish data.[2..]
            (*writer(BitConverter.GetBytes 0x7dus)
            writer(Array.concat [|
                BitConverter.GetBytes 0x0360us
                BitConverter.GetBytes 1
            |])*)            
        | 0x0081us -> Logger.Error ("Forced disconnect. Code %d", data.[2])
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *) -> ()        
        | unknown -> Logger.Error("Unhandled packet {packetType:X} with length {length}", unknown, data.Length) //shutdown()
    handler
