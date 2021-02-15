module Yggdrasil.PacketStream.Null

open Yggdrasil.IO
open Yggdrasil.Types
open Yggdrasil.PacketStream.Observer

let NullStream tick stream =
    Observable.map(fun (pType, _) ->
        match pType with
        | 0x0091us -> Outgoing.OnlineRequest tick stream DoneLoadingMap; Skip
        | 0x283us (* WantToConnect ack *)
        | 0x0adfus (* ZC_REQNAME_TITLE *)
        | 0x121us (* cart info *)
        | 0x0a9bus (* list of items in the equip switch window *)  
        | 0x00b4us (* ZC_SAY_DIALOG *)
        | 0x00b5us (* ZC_WAIT_DIALOG *)
        | 0x00b7us (* ZC_MENU_LIST *)
        | 0x0a30us (* ZC_ACK_REQNAMEALL2 *)        
        | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *)
        | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *)
        | 0xa24us (* ZC_ACH_UPDATE *)
        | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *)
        | 0x2c9us (* ZC_PARTY_CONFIG *)
        | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *)
        | 0x00b6us (* ZC_CLOSE_DIALOG *)
        | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *)
        | 0x01c3us (* ZC_BROADCAST2 *)
        | 0x099aus (* ZC_ACK_TAKEOFF_EQUIP_V5 *)
        | 0x0999us (* ZC_ACK_WEAR_EQUIP_V5 *)
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> Skip
        | _ -> Unhandled pType
    )