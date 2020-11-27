module Yggdrasil.IO.Incoming

open System
open NLog
open Yggdrasil.Communication
open Yggdrasil.PacketTypes
open Yggdrasil.Utils

let Logger = LogManager.GetCurrentClassLogger()

let OnParameterChange (mailbox: AgentMailbox) parameter value =
    match parameter with
    | Parameter.Weight | Parameter.MaxWeight | Parameter.SkillPoints | Parameter.StatusPoints
    | Parameter.JobLevel | Parameter.BaseLevel | Parameter.MaxHP | Parameter.MaxSP
    | Parameter.SP | Parameter.HP -> mailbox.Post <| StatusU32 (parameter, ToUInt32 value)
    
    | Parameter.Manner | Parameter.Hit | Parameter.Flee1
    | Parameter.Flee2 | Parameter.Critical -> mailbox.Post <| StatusI16 (parameter, ToInt16 value)
    
    | Parameter.Speed | Parameter.AttackSpeed | Parameter.Attack1 | Parameter.Attack2
    | Parameter.Defense1 | Parameter.Defense2 | Parameter.MagicAttack1
    | Parameter.MagicAttack2 | Parameter.MagicDefense1 | Parameter.MagicDefense2
    | Parameter.AttackRange -> mailbox.Post <| StatusU16 (parameter, ToUInt16 value)
    
    | Parameter.Zeny | Parameter.USTR |Parameter.UAGI |Parameter.UDEX
    | Parameter.UVIT |Parameter.ULUK |Parameter.UINT -> mailbox.Post <| StatusI32 (parameter, ToInt32 value)
    
    | Parameter.JobExp | Parameter.NextBaseExp
    | Parameter.BaseExp | Parameter.NextJobExp -> mailbox.Post <| Status64 (parameter, ToInt64 value)
    
    | Parameter.STR |Parameter.AGI |Parameter.DEX | Parameter.VIT
    | Parameter.LUK |Parameter.INT -> mailbox.Post <| StatusPair (parameter, ToUInt16 value.[2..], ToInt16 value.[6..])
    
    | Parameter.Karma -> ()
    
    | _ -> Logger.Error("Unhandled parameter: {paramCode}", parameter)
    
let OnWeightSoftCap (mailbox: AgentMailbox) value =
    mailbox.Post(WeightSoftCap <| ToInt32 value)
   
let ZonePacketHandler (mailbox: AgentMailbox) writer =
    let rec handler (packetType: uint16) (data: byte[]) =
        match packetType with
        | 0x13aus -> OnParameterChange mailbox Parameter.AttackRange data.[2..] 
        //| 0x121us (* cart info *) -> messenger.Post <| StatusUpdate (data.[2..] |> ToUInt16, data.[6..] |> ToInt32)
        | 0x00b0us -> OnParameterChange mailbox (data.[2..] |> ToParameter)  data.[4..] 
        | 0x0141us -> OnParameterChange mailbox (data.[2..] |> ToParameter)  data.[4..]
        | 0xacbus -> OnParameterChange mailbox (data.[2..] |> ToParameter)  data.[4..]
        | 0xadeus -> mailbox.Post(WeightSoftCap <| ToInt32 data)
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> ()
        | 0x007fus (* ZC_NOTIFY_TIME *)  -> ()
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        //| 0x9ffus -> SpawnNonPlayer robot.Agent data.[4..]
        //| 0x9feus -> SpawnPlayer robot.Agent data.[4..]
        //| 0x10fus -> AddSkill robot.Agent data.[4..]
        //| 0x0087us -> StartWalk robot.Agent data.[2..]
        //| 0x080eus -> PartyMemberHPUpdate robot.Agent data.[2..]
        | 0x2ebus ->
            writer(BitConverter.GetBytes 0x7dus)
            writer(Array.concat [|
                BitConverter.GetBytes 0x0360us
                BitConverter.GetBytes 1
            |])            
        | 0x0081us -> printfn "Forced disconnect. Code %d" data.[2]
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *) -> ()        
        | unknown -> Logger.Error("[{accountId}] Unhandled packet {packetType:X} with length {length}",0, unknown, data.Length) //shutdown()
    handler
