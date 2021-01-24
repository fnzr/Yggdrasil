module Yggdrasil.IO.Incoming

open System
open NLog
open Yggdrasil.Game.Components
open Yggdrasil.Game.Event
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
open Yggdrasil.IO.Decoder
open Yggdrasil.Pipe.Attributes
open Yggdrasil.Pipe.Location
open Yggdrasil.Pipe.Spawn
open Yggdrasil.Pipe.Combat
open Yggdrasil.Pipe.Item
open FSharpPlus.Lens
let Logger = LogManager.GetLogger "Incoming"

let ConnectionAccepted position serverTick world =
    {world with
        TickOffset =  (Connection.Tick()) - serverTick
        Player = setl Player._Position position world.Player
        IsConnected = true
    }
    
let Disconnected (code: byte) world =
    Logger.Warn ("Forced disconnect. Code {code}", code);
    {world with IsConnected = false}
    
let UpdateTickOffset serverTick world =
    {world with TickOffset = serverTick - Connection.Tick()}
    
let ParseEquipItem data =
    let parse bytes =
        let (equip, leftover) = MakePartialRecord<RawEquipItemBase> bytes [||]
        let options =
            //not actually sure if this option count is worth something
            //maybe it's always 0 but server always(?) sends 5
            //maybe it's a real value that just defaults to 5
            [0 .. int equip.OptionCount]
            |> List.map (fun i -> leftover.[i*5..])
            |> List.fold (fun t e -> MakeRecord<RawEquipItemOption> e :: t) []
        {
            Base = equip            
            Options = options
            Flags = {
                IsIdentified = bytes.[56] &&& 1uy = 1uy
                IsDamaged = bytes.[56] &&& 2uy = 2uy
                //didnt check this one, not like I'm gonna use it
                PlaceEtcTab = bytes.[56] &&& 4uy = 4uy
            }
        }
    data 
    |> Array.chunkBySize 57
    |> Array.map parse
    |> Array.map Equipment.FromRaw
    |> Array.toList
           
let PacketReceiver callback (packetType: uint16, (packetData: ReadOnlyMemory<byte>)) =
    let data = packetData.ToArray()
    Logger.Trace("Packet {packetType:X}", packetType)
    try 
        let pipeOpt =
            match packetType with
            | 0x13aus -> Some <| ParameterChange Parameter.AttackRange data.[2..]
            | 0x00b0us -> Some <| ParameterChange (data.[2..] |> ToParameter) data.[4..] 
            | 0x0141us -> Some <| ParameterChange (data.[2..] |> ToParameter) data.[4..]
            | 0xacbus -> Some <| ParameterChange (data.[2..] |> ToParameter) data.[4..]        
            | 0xadeus -> Some <| WeightSoftCap (ToInt32 data.[2..])
            | 0x9ffus | 0x9feus | 0x9fdus ->
                //skip MoveStartTime (uint32) for moving units
                Some <| 
                let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
                let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
                let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
                UnitSpawn (CreateNonPlayer part1 part2 (int x, int y))
            | 0x0080us ->
                Some <| 
                UnitDisappear (ToUInt32 data.[2..])
                    (Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason)
            | 0x10fus ->                
                 //TODO SkillRaw -> Skill
                data.[4..]
                |> Array.chunkBySize 37
                |> Array.map (fun s -> fst <| MakePartialRecord<Skill> s [|24|])
                |> Array.toList
                |> AddSkills
                |> Some
            | 0x0087us ->
                let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
                Some <| PlayerWalk (int x0, int y0) (int x1, int y1) (int64 <| ToUInt32 data.[2..]) callback
            | 0x0086us ->
                let id = ToUInt32 data.[2..]
                let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]            
                Some <| UnitWalk id (int x0, int y0) (int x1, int y1) (int64 <| ToUInt32 data.[12..]) callback
            | 0x07fbus -> Some <| SkillCast (MakeRecord<RawSkillCast> data.[2..]) callback
            | 0x0088us -> Some <| MoveUnit (MakeRecord<UnitMove> data.[2..]) callback
            | 0x0977us -> Some <| UpdateMonsterHP (MakeRecord<MonsterHPInfo> data.[2..])        
            | 0x008aus -> Some <| DamageDealt (MakeRecord<RawDamageInfo> data.[2..]) callback
            | 0x08c8us -> Some <| DamageDealt2 (MakeRecord<RawDamageInfo2> data.[2..]) callback        
            | 0x0addus -> Some <| AddItemDrop (MakeRecord<ItemDropRaw> data.[2..])
            | 0x00a1us -> Some <| RemoveItemDrop (ToInt32 data.[2..])        
            | 0x2ebus ->
                let (x, y, _) = UnpackPosition data.[6..]
                Some <| ConnectionAccepted (int x, int y) (int64 (ToUInt32 data.[2..]))
            | 0x0091us ->
                let position = (data.[18..] |> ToUInt16 |> int,
                                data.[20..] |> ToUInt16 |> int)
                let map = (let gatFile = ToString data.[..17]
                   gatFile.Substring(0, gatFile.Length - 4))
                Some <| MapChange position map        
            | 0x007fus -> Some <| UpdateTickOffset (int64 (ToUInt32 data.[2..]))
            | 0x00bdus -> Some <| InitialCharacterStatus (MakeRecord<CharacterStatusRaw> data.[2..])
            | 0x0081us -> Some <| Disconnected data.[2]
            | 0x099bus -> Some <| MapProperty (ToInt16 data.[2..]) (ToInt32 data.[4..])
            | 0x080eus -> Some <| UpdatePartyMemberHP (ToUInt32 data.[2..]) (ToInt32 data.[6..]) (ToInt32 data.[10..])
            | 0xa0dus  -> Some <| AddEquipment (ParseEquipItem data.[4..])
            | 0x0adfus (* ZC_REQNAME_TITLE *) -> None
            | 0x121us (* cart info *) -> None
            | 0x0a9bus (* list of items in the equip switch window *) -> None    
            | 0x00b4us (* ZC_SAY_DIALOG *) -> None
            | 0x00b5us (* ZC_WAIT_DIALOG *) -> None
            | 0x00b7us (* ZC_MENU_LIST *) -> None
            | 0x0a30us (* ZC_ACK_REQNAMEALL2 *) -> None
            | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
            | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
            | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
            | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
            | 0x00c0us (* ZC_EMOTION *) -> None
            | 0x01c3us (* ZC_BROADCAST2 *) -> None
            | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length); None
        
        match pipeOpt with
        | Some pipe -> callback pipe
        | None -> ()
    with
    | e -> Logger.Error e
