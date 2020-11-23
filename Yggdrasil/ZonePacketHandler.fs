module Yggdrasil.ZoneService

open System
open NLog
open Yggdrasil.Structure
open Yggdrasil.Robot
open Yggdrasil.Utils
open Yggdrasil.ZoneHelper

let Logger = LogManager.GetCurrentClassLogger()

let ZonePacketHandler (robot: Robot) writer =
    let rec handler (packetType: uint16) (data: byte[]) =
        Logging.LogPacket robot.AccountId packetType data
        match packetType with
        | 0x13aus -> robot.Agent.Post(ParameterChange ("AttackRange", ToUInt16 data.[2..] |> int))
        | 0x121us -> robot.Agent.Post(ParameterChange ((ToInt32 data.[2..] |> ToParameterName), ToInt32 data.[6..]))
        | 0x00b0us -> robot.Agent.Post(ParameterChange ("Weight", ToInt32 data.[4..]))
        //Ignore plus bonus, its redundant
        | 0x0141us -> robot.Agent.Post(ParameterChange ((ToInt32 data.[2..] |> ToParameterName), ToInt32 data.[6..]))
        | 0xacbus -> robot.Agent.Post(ParameterLongChange ((ToInt16 data.[2..] |> int |> ToParameterName), ToInt64 data.[4..]))
        | 0xadeus -> robot.Agent.Post(WeightSoftCap (ToInt32 data.[2..]))
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> ()
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> ()
        | 0x007fus (* ZC_NOTIFY_TIME *)  -> ()
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()        
        | 0x9ffus -> SpawnNPC robot.Agent data.[4..]
        | 0x9feus -> SpawnPlayer robot.Agent data.[4..]
        | 0x10fus -> AddSkill robot.Agent data.[4..]
        | 0x0087us -> StartWalk robot.Agent data.[2..]
        | 0x080eus -> PartyMemberHPUpdate robot.Agent data.[2..]
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
        | unknown -> Logger.Error("[{accountId}] Unhandled packet {packetType:X} with length {length}", robot.AccountId, unknown, data.Length) //shutdown()
    handler

let ClientPacketHandler (robot: Robot) =
    let rec handler (packetType: uint16) (data: byte[]) =
        Logger.Info("[{accountId}] Received packet {packetType:X} with length {length}",
                     robot.AccountId, packetType, data.Length)
    handler