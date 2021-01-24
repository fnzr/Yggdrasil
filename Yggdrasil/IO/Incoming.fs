module Yggdrasil.IO.Incoming

open System
open NLog
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
    
let UpdateTickOffset serverTick world =
    {world with TickOffset = (Connection.Tick()) - serverTick}
           
let PacketReceiver callback (packetType, (packetData: ReadOnlyMemory<byte>)) =
    let data = packetData.ToArray()
    callback <|
        match packetType with
        | 0x13aus -> ParameterChange Parameter.AttackRange data.[2..]
        | 0x00b0us -> ParameterChange (data.[2..] |> ToParameter) data.[4..] 
        | 0x0141us -> ParameterChange (data.[2..] |> ToParameter) data.[4..]
        | 0xacbus -> ParameterChange (data.[2..] |> ToParameter) data.[4..]        
        | 0xadeus -> WeightSoftCap (ToInt32 data.[2..])
        | 0x9ffus | 0x9feus | 0x9fdus ->
            //skip MoveStartTime (uint32) for moving units
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
            let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
            UnitSpawn (CreateNonPlayer part1 part2 (int x, int y))
        | 0x0080us ->
            UnitDisappear (ToUInt32 data.[2..])
                (Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason)
        | 0x10fus ->
            let rec ParseSkill bytes skills =        
                match bytes with
                | [||] -> skills 
                | _ ->
                //TODO SkillRaw -> Skill
                let (skill, leftover) = MakePartialRecord<Skill> data [|24|]
                ParseSkill leftover (skill :: skills)
            AddSkills (ParseSkill data.[4..] [])        
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            PlayerWalk (int x0, int y0) (int x1, int y1) (int64 <| ToUInt32 data.[2..]) callback
        | 0x0086us ->
            let id = ToUInt32 data.[2..]
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]            
            UnitWalk id (int x0, int y0) (int x1, int y1) (int64 <| ToUInt32 data.[12..]) callback
        | 0x07fbus -> SkillCast (MakeRecord<RawSkillCast> data.[2..]) callback
        | 0x0088us -> MoveUnit (MakeRecord<UnitMove> data.[2..]) callback
        | 0x0977us -> UpdateMonsterHP (MakeRecord<MonsterHPInfo> data.[2..])        
        | 0x008aus -> DamageDealt (MakeRecord<RawDamageInfo> data.[2..]) callback
        | 0x08c8us -> DamageDealt2 (MakeRecord<RawDamageInfo2> data.[2..]) callback        
        | 0x0addus -> AddItemDrop (MakeRecord<ItemDropRaw> data.[2..])
        | 0x00a1us -> RemoveItemDrop (ToInt32 data.[2..])        
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]
            ConnectionAccepted (int x, int y) (int64 (ToUInt32 data.[2..]))
        | 0x0091us ->
            let position = (data.[18..] |> ToUInt16 |> int,
                            data.[20..] |> ToUInt16 |> int)
            let map = (let gatFile = ToString data.[..17]
               gatFile.Substring(0, gatFile.Length - 4))
            MapChange position map        
        | 0x007fus -> UpdateTickOffset (int64 (ToUInt32 data.[2..]))
        | 0x0bdus -> InitialCharacterStatus (MakeRecord<CharacterStatusRaw> data.[2..])
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> MapProperty (ToInt16 data.[2..]) (ToInt32 data.[4..])
        | 0x0adfus (* ZC_REQNAME_TITLE *) -> id
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> id        
        | 0x121us (* cart info *) -> id
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> id
        | 0x0a9bus (* list of items in the equip switch window *) -> id        
        | 0x00b4us (* ZC_SAY_DIALOG *) -> id
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> id
        | 0x00b7us (* ZC_MENU_LIST *) -> id
        | 0x0a30us (* ZC_ACK_REQNAMEALL2 *) -> id
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *) -> id
        | 0x0081us -> id//Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length);
                        id
