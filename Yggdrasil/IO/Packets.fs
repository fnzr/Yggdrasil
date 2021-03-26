module Yggdrasil.IO.Packets

open System
open NLog
open Yggdrasil.Types
open Yggdrasil.IO.RawTypes
open Yggdrasil.IO.Decoder
open Yggdrasil.Game
let Logger = LogManager.GetLogger "UnitStream"

let ToAttribute (param: RawAttribute) =
    let name = Enum.GetName(typeof<RawAttribute>, param)
    Enum.Parse(typeof<Attribute>, name) :?> Attribute

let EquipmentFromRaw (raw: RawEquipItem) = {
    Id = raw.Base.Id
    Index = raw.Base.Index
    Type = raw.Base.Type
    Location = raw.Base.Location
    WearState = raw.Base.WearState
    IsIdentified = raw.Flags.IsIdentified
    IsDamaged = raw.Flags.IsDamaged
}
let CreateEquipment data =

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
    |> Array.map EquipmentFromRaw
    |> Array.toList

let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) =
        let (x, y, _) = UnpackPosition [|raw2.PosPart1; raw2.PosPart2; raw2.PosPart3|]
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> EntityType.NPC
                    | 0x0uy -> EntityType.PC
                    | 0x5uy -> EntityType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            EntityType.Invalid
        let messages =
            [
                {
                    Id = raw1.AID
                    Type = oType
                    Name = raw2.Name.Split("#").[0]
                } |> New
                Speed (raw1.AID, float raw1.Speed)
                {
                    Id = raw1.AID
                    Coordinates = (x, y)
                } |> Position

            ]
        Messages <|
            match oType with
            | EntityType.PC | EntityType.Monster ->
                ({
                    Id = raw1.AID
                    MaxHP = raw2.MaxHP
                    HP = raw2.HP
                } |> Health) :: messages
            | _ -> messages

let Observer playerId tick =
    let mutable tickOffset = 0L
    Observable.map(fun (pType, (pData: ReadOnlyMemory<_>)) ->
        let data = pData.ToArray()
        match pType with
        | 0x0091us ->
            //request DoneLoadingMap; Skip
            let map = Yggdrasil.Navigation.Maps.GetMap
                       (let gatFile = ToString data.[..17]
                    gatFile.Substring(0, gatFile.Length - 4))
            [
                MapChanged map.Name
                {
                    Id = playerId
                    Coordinates = (data.[18..] |> ToInt16, data.[20..] |> ToInt16)
                } |> Position
            ] |> Messages
        | 0x9feus | 0x9ffus ->
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
            CreateNonPlayer part1 part2
        | 0x9fdus ->
            //WalkingUnit appear. Skip MoveStartTime in the packet
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover.[4..] [|24|]
            CreateNonPlayer part1 part2
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]
            tickOffset <- int64 (ToUInt32 data.[2..])
            {
                Id = playerId
                Coordinates = (x, y)
            } |> Position |> Message
        | 0x0088us ->
            let info = MakeRecord<UnitMove2> data.[2..]
            {
                Id = info.Id
                Coordinates = (info.X, info.Y)
            } |> Position |> Message
        | 0x0080us ->
            //TODO handle disappear reason
            let reason = Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason
            {
                Id = ToUInt32 data.[2..]
                Coordinates = (-1s, -1s)
            } |> Position |> Message
            //(ToUInt32 data.[2..], Disappear reason) |> mailbox
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[2..]) + tickOffset |> float
            {
                Id = playerId
                Origin = x0, y0
                Target = x1, y1
                Delay = if delay < 0.0 then 0.0 else delay
            } |> Movement |> Message
        | 0x0086us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[12..]) + tickOffset |> float
            let id = ToUInt32 data.[2..]
            {
                Id = id
                Origin = x0, y0
                Target = x1, y1
                Delay = if delay < 0.0 then 0.0 else delay
            } |> Movement |> Message
        | 0x141us ->
            let param = data.[2..] |> ToRawAttribute |> ToAttribute
            let value = data.[6..] |> ToInt32
            let bonus = data.[10..] |> ToInt32
            Parameter [param, value + bonus] |> Message
        | 0x13aus ->
            Parameter [Attribute.AttackSpeed1, data.[2..] |> ToInt16 |> int] |> Message
        | 0x10fus ->
            //TODO SkillRaw -> Skill
            data.[4..]
            |> Array.chunkBySize 37
            |> Array.map (fun s -> fst <| MakePartialRecord<Skill> s [|24|])
            |> Array.toList
            |> NewSkill |> Message
        | 0x00bdus ->
            //TODO: Battle parameters
            let info = MakeRecord<CharacterStatusRaw> data.[2..]
            [
                Parameter [
                    Attribute.StatusPoints, int info.Points
                    Attribute.STR, int info.STR
                    Attribute.AGI, int info.AGI
                    Attribute.DEX, int info.DEX
                    Attribute.INT, int info.INT
                    Attribute.LUK, int info.LUK
                    Attribute.VIT, int info.VIT
                    Attribute.USTR, int info.USTR
                    Attribute.UAGI, int info.UAGI
                    Attribute.UDEX, int info.UDEX
                    Attribute.UINT, int info.UINT
                    Attribute.ULUK, int info.ULUK
                    Attribute.UVIT, int info.UVIT
                    Attribute.Attack1, int info.ATK
                    Attribute.Attack2, int info.ATK2
                    Attribute.MagicAttack1, int info.MATK_MIN
                    Attribute.MagicAttack2, int info.MATK_MAX
                    Attribute.Defense1, int info.DEF
                    Attribute.Defense2, int info.DEF2
                    Attribute.MagicDefense1, int info.MDEF
                    Attribute.MagicDefense2, int info.MDEF2
                    Attribute.Hit, int info.HIT
                    Attribute.Flee1, int info.FLEE
                    Attribute.Flee2, int info.FLEE2
                    Attribute.Critical, int info.CRIT
                    Attribute.AttackSpeed1, int info.ASPD
                    Attribute.AttackSpeed2, int info.ASPD2
                ]
            ] |> Messages
        | 0x0acbus ->
            let value = ToInt64 data.[4..]
            match ToRawAttribute data.[2..] with
            | RawAttribute.BaseExp -> BaseExp value |> Message
            | RawAttribute.NextBaseExp -> ExpNextBaseLevel value |> Message
            | RawAttribute.JobExp -> JobExp value |> Message
            | RawAttribute.NextJobExp -> ExpNextJobLevel value |> Message
            | _ -> Skip
        | 0xadeus -> data.[2..] |> ToInt32 |> WeightSoftCap |> Message
        | 0xa0dus -> data.[4..] |> CreateEquipment |> Equipment |> Message
        | 0x283us (* WantToConnect ack *) -> Connected true |> Message
        | 0x00b0us ->
            let value = ToInt32 data.[4..]
            match ToRawAttribute data.[2..] with
            | RawAttribute.Speed -> (playerId , float value) |> Speed |> Message
            | RawAttribute.HP -> (playerId, value) |> HP |> Message
            | RawAttribute.MaxHP -> (playerId, value) |> MaxHP |> Message
            | param -> [(ToAttribute param, value)] |> Parameter |> Message
        | 0x80eus ->
            (*
            let info = MakeRecord<PartyHP> data.[2..]
            ({
                Id = info.AccountId
                MaxHP = info.MaxHP
                HP = info.HP
            }: Yggdrasil.Pipe.Health.Health)
            |> Yggdrasil.Pipe.Health.HealthUpdate.Update
            |> postHealth
            *)
            Skip
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
