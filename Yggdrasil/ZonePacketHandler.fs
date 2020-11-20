module Yggdrasil.ZoneService

open System
open Yggdrasil.Structure
open Yggdrasil.Robot
open Yggdrasil.Utils
open Yggdrasil.ZonePacketHandler

let ZonePacketHandler (robot: Robot) writer shutdown =
    let rec handler (packetType: uint16) (data: byte[]) =
        Logging.LogPacket robot.AccountId packetType data
        match packetType with        
        | 0x00b0us -> ParameterChange robot.Agent (ToUInt16 data.[2..]) (ToUInt32 data.[4..])
        | 0x0141us -> StatusChange robot.Agent (ToUInt16 data.[4..]) (ToUInt32 data.[6..]) (ToUInt32 data.[10..])
        | 0xadeus -> robot.Agent.Post(WeightSoftCap (ToInt32 data.[2..]))
        | 0x0091us (* ZC_NPCACK_MAPMOVE *) -> () 
        | 0x2ebus ->
            writer(BitConverter.GetBytes 0x7dus)
            writer(Array.concat [|
                BitConverter.GetBytes 0x0360us
                BitConverter.GetBytes 1
            |])
        | 0x0081us -> printfn "Forced disconnect. Code %d" data.[2]
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) -> ()
        | unknown -> printfn "Unhandled packet %X with length %d" unknown data.Length; shutdown()
    handler

