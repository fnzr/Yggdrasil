module Yggdrasil.IO.Incoming

open System
open FSharp.Control.Reactive
open NLog
open Yggdrasil.Game.Components
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
open Yggdrasil.IO.Decoder
open FSharp.Control.Reactive.Builders
open Yggdrasil.Pipe.Attributes
open Yggdrasil.Pipe.Location
open Yggdrasil.Pipe.Spawn
open Yggdrasil.Pipe.Combat
open Yggdrasil.Pipe.Item
let Logger = LogManager.GetLogger "Incoming"

let ConnectionAccepted position serverTick (game: Game) =
    let player = {game.Player with Position = position}
    {game with
        TickOffset =  (Connection.Tick()) - serverTick
        Units = game.Units.Add(player.Id, player)
        IsConnected = true
    }
    
let Disconnected (code: byte) game =
    Logger.Warn ("Forced disconnect. Code {code}", code);
    {game with IsConnected = false}
    
let UpdateTickOffset serverTick game =
    {game with TickOffset = serverTick - Connection.Tick()}
    
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

let PacketParser playerId =
    Observable.flatmap <|
    fun (packetType, packetData: ReadOnlyMemory<_>) -> observe {
        invalidArg "invalid!" "oops"
        let data = packetData.ToArray()
        match packetType with                    
        | 0xadeus -> yield WeightSoftCap (ToInt32 data.[2..])
        | 0x9ffus | 0x9feus | 0x9fdus ->
            //skip MoveStartTime (uint32) for moving units
            yield (
                let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
                let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
                let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
                CreateNonPlayer part1 part2 (x, y)
                |> NewUnit |> UnitUpdate)
        | 0x0080us ->
            yield (
                (ToUInt32 data.[2..],
                    Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason)
                |> DelUnit |> UnitUpdate)
        | 0x10fus ->                
             //TODO SkillRaw -> Skill
            yield
                (data.[4..]
                |> Array.chunkBySize 37
                |> Array.map (fun s -> fst <| MakePartialRecord<Skill> s [|24|])
                |> Array.toList
                |> NewSkills |> SkillUpdate)
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            yield UnitPosition (playerId, (x0, y0))
            yield PlayerMovement {
                Destination = (x1, y1)
                TimeStart = Some (int64 <| ToUInt32 data.[12..])
                }
        | 0x0086us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let id = ToUInt32 data.[2..]
            yield UnitPosition (id, (x0, y0))
            yield UnitMovement (id, {
                Destination = (x1, y1)
                TimeStart = Some (int64 <| ToUInt32 data.[12..])
            })
        //| 0x07fbus -> yield SkillCast (MakeRecord<RawSkillCast> data.[2..]) callback
        | 0x0088us ->
            let info = MakeRecord<UnitMove2> data.[2..]
            yield UnitPosition (info.Id, (info.X, info.Y))
        //| 0x0977us -> yield UpdateMonsterHP (MakeRecord<MonsterHPInfo> data.[2..])        
        //| 0x008aus -> yield DamageDealt (MakeRecord<RawDamageInfo> data.[2..]) callback
        //| 0x08c8us -> yield DamageDealt2 (MakeRecord<RawDamageInfo2> data.[2..]) callback        
        //| 0x0addus -> yield AddDroppedItem (MakeRecord<ItemDropRaw> data.[2..])
        //| 0x00a1us -> yield RemoveDroppedItem (ToInt32 data.[2..])        
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]
            yield UnitPosition (playerId, (x, y))
            yield TickOffset (int64 (ToUInt32 data.[2..]))
            yield IsConnected true
        | 0x0091us ->
            let position = (data.[18..] |> ToInt16,
                            data.[20..] |> ToInt16)
            let map = (let gatFile = ToString data.[..17]
               gatFile.Substring(0, gatFile.Length - 4))
            yield MapChanged map
            yield UnitPosition (playerId, position)                    
        //| 0x007fus -> yield UpdateTickOffset (int64 (ToUInt32 data.[2..]))
        //| 0x00bdus -> yield InitialCharacterStatus (MakeRecord<CharacterStatusRaw> data.[2..])
        | 0x0081us -> yield IsConnected false //yield Disconnected data.[2]            
        //| 0x099bus -> yield MapProperty (ToInt16 data.[2..]) (ToInt32 data.[4..])
        //| 0x080eus -> yield UpdatePartyMemberHP (ToUInt32 data.[2..]) (ToInt32 data.[6..]) (ToInt32 data.[10..])
        //| 0xa0dus  -> yield AddGear (ParseEquipItem data.[4..])
        | 0x13aus -> () //yield ParameterChange Parameter.AttackRange data.[2..]
        | 0x00b0us -> () //yield ParameterChange (data.[2..] |> ToParameter) data.[4..] 
        | 0x0141us -> () //yield ParameterChange (data.[2..] |> ToParameter) data.[4..]
        | 0xacbus -> () //yield ParameterChange (data.[2..] |> ToParameter) data.[4..]
        | 0x0adfus (* ZC_REQNAME_TITLE *) -> ()
        | 0x121us (* cart info *) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()    
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x0a30us (* ZC_ACK_REQNAMEALL2 *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *) -> ()
        | 0x01c3us (* ZC_BROADCAST2 *) -> ()
        | 0x099aus (* ZC_ACK_TAKEOFF_EQUIP_V5 *) -> ()
        | 0x0999us (* ZC_ACK_WEAR_EQUIP_V5 *) -> ()
        | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length); ()
    }
        
           
let PacketReceiver callback (packetType: uint16, (packetData: ReadOnlyMemory<byte>)) =
    let data = packetData.ToArray()
    Logger.Trace("Packet {packetType:X}", packetType)
    try 
        match packetType with                    
        | 0xadeus -> WeightSoftCap (ToInt32 data.[2..]) |> callback
        | 0x9ffus | 0x9feus | 0x9fdus ->
            //skip MoveStartTime (uint32) for moving units                 
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
            let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
            CreateNonPlayer part1 part2 (x, y)
            |> NewUnit |> UnitUpdate |> callback
        | 0x0080us ->
            (ToUInt32 data.[2..],
                Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason)
            |> DelUnit |> UnitUpdate |> callback
        | 0x10fus ->                
             //TODO SkillRaw -> Skill
            data.[4..]
            |> Array.chunkBySize 37
            |> Array.map (fun s -> fst <| MakePartialRecord<Skill> s [|24|])
            |> Array.toList
            |> NewSkills |> SkillUpdate |> callback
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            PlayerPosition (x0, y0) |> callback
            PlayerMovement {
                Destination = (x1, y1)
                TimeStart = Some (int64 <| ToUInt32 data.[12..])
            } |> callback
        | 0x0086us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let id = ToUInt32 data.[2..]
            UnitPosition (id, (x0, y0)) |> callback
            UnitMovement (id, {
                Destination = (x1, y1)
                TimeStart = Some (int64 <| ToUInt32 data.[12..])
            }) |> callback
        //| 0x07fbus -> Some <| SkillCast (MakeRecord<RawSkillCast> data.[2..]) callback
        | 0x0088us ->
            let info = MakeRecord<UnitMove2> data.[2..]
            UnitPosition (info.Id, (info.X, info.Y)) |> callback
        //| 0x0977us -> Some <| UpdateMonsterHP (MakeRecord<MonsterHPInfo> data.[2..])        
        //| 0x008aus -> Some <| DamageDealt (MakeRecord<RawDamageInfo> data.[2..]) callback
        //| 0x08c8us -> Some <| DamageDealt2 (MakeRecord<RawDamageInfo2> data.[2..]) callback        
        //| 0x0addus -> Some <| AddDroppedItem (MakeRecord<ItemDropRaw> data.[2..])
        //| 0x00a1us -> Some <| RemoveDroppedItem (ToInt32 data.[2..])        
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]
            callback <| PlayerPosition (x, y)
            callback <| TickOffset (int64 (ToUInt32 data.[2..]))
            callback <| IsConnected true
        | 0x0091us ->
            let position = (data.[18..] |> ToInt16,
                            data.[20..] |> ToInt16)
            let map = (let gatFile = ToString data.[..17]
               gatFile.Substring(0, gatFile.Length - 4))
            MapChanged map |> callback
            PlayerPosition position |> callback                    
        //| 0x007fus -> Some <| UpdateTickOffset (int64 (ToUInt32 data.[2..]))
        //| 0x00bdus -> Some <| InitialCharacterStatus (MakeRecord<CharacterStatusRaw> data.[2..])
        | 0x0081us -> IsConnected false |> callback //Some <| Disconnected data.[2]            
        //| 0x099bus -> Some <| MapProperty (ToInt16 data.[2..]) (ToInt32 data.[4..])
        //| 0x080eus -> Some <| UpdatePartyMemberHP (ToUInt32 data.[2..]) (ToInt32 data.[6..]) (ToInt32 data.[10..])
        //| 0xa0dus  -> Some <| AddGear (ParseEquipItem data.[4..])
        | 0x13aus -> () //Some <| ParameterChange Parameter.AttackRange data.[2..]
        | 0x00b0us -> () //Some <| ParameterChange (data.[2..] |> ToParameter) data.[4..] 
        | 0x0141us -> () //Some <| ParameterChange (data.[2..] |> ToParameter) data.[4..]
        | 0xacbus -> () //Some <| ParameterChange (data.[2..] |> ToParameter) data.[4..]
        | 0x0adfus (* ZC_REQNAME_TITLE *) -> ()
        | 0x121us (* cart info *) -> ()
        | 0x0a9bus (* list of items in the equip switch window *) -> ()    
        | 0x00b4us (* ZC_SAY_DIALOG *) -> ()
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> ()
        | 0x00b7us (* ZC_MENU_LIST *) -> ()
        | 0x0a30us (* ZC_ACK_REQNAMEALL2 *) -> ()
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *) -> ()
        | 0x01c3us (* ZC_BROADCAST2 *) -> ()
        | 0x099aus (* ZC_ACK_TAKEOFF_EQUIP_V5 *) -> ()
        | 0x0999us (* ZC_ACK_WEAR_EQUIP_V5 *) -> ()
        | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length); ()
    with
    | e -> Logger.Error e
