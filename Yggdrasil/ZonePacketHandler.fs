module Yggdrasil.ZoneService

open System
open NLog
open Yggdrasil.Agent
open Yggdrasil.PacketTypes
open Yggdrasil.Robot
open Yggdrasil.Utils

let Logger = LogManager.GetCurrentClassLogger()


   
let ZonePacketHandler (messenger: Messenger) writer =
    let rec handler (packetType: uint16) (data: byte[]) =
        match packetType with
        | 0x13aus -> messenger.Post <| StatusUpdate (1000us, data.[2..] |> ToUInt16 |> int) 
        //| 0x121us (* cart info *) -> messenger.Post <| StatusUpdate (data.[2..] |> ToUInt16, data.[6..] |> ToInt32)
        | 0x00b0us -> messenger.Post <| StatusUpdate (data.[2..] |> ToUInt16,  data.[4..] |> ToInt32) 
        //Ignore plus bonus, its redundant
        | 0x0141us -> messenger.Post <| StatusUpdate (data.[2..] |> ToUInt16,  data.[4..] |> ToInt32)
        | 0xacbus -> messenger.Post <| Status64Update (ToUInt16 data.[2..], ToInt64 data.[4..])
        //| 0xadeus -> robot.Agent.Post(WeightSoftCap (ToInt32 data.[2..]))
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

let ClientPacketHandler (robot: Robot) =
    let rec handler (packetType: uint16) (data: byte[]) =
        Logger.Info("[{accountId}] Received packet {packetType:X} with length {length}",
                     robot.AccountId, packetType, data.Length)
    handler