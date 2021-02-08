module Yggdrasil.IO.Incoming

open NLog
let Logger = LogManager.GetLogger "Incoming"
(*
let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) position map (post: Id -> SentryReport -> unit) =        
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> EntityType.NPC
                    | 0x0uy -> EntityType.PC
                    | 0x5uy -> EntityType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            EntityType.Invalid
        post raw1.AID <| EntityPosition {
            Id = raw1.AID
            Name = raw2.Name.Split("#").[0]
            Map = map
            Coordinates = position
        }
        if oType <> EntityType.NPC then
            post raw1.AID <| EntityHealth {
                MaxHP = raw2.MaxHP
                HP = raw2.HP
            }
            post raw1.AID <| Speed (float raw1.Speed) 
    
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
    //|> Array.map Equipment.FromRaw
    //|> Array.toList
 *) (* 
let HandlePacket agentId tickOffset time (update: Id -> SentryReport -> unit) packetType (data: _[]) =
    match packetType with
    | 0xadeus -> ToInt32 data.[2..] |> WeightSoftCap |> update agentId
    | 0x9ffus | 0x9feus | 0x9fdus ->
        //skip MoveStartTime (uint32) for moving units
        let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
        let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
        let (x, y, _) = UnpackPosition [|part2.PosPart1; part2.PosPart2; part2.PosPart3|]
        let unit = CreateNonPlayer part1 part2 (x, y)
        update unit.Id (EntityPosition unit)
    | 0x0080us ->
        let id = ToUInt32 data.[2..]
        let reason = Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason
        update id (LostUnit reason)
    | 0x10fus ->                
        //TODO SkillRaw -> Skill
        data.[4..]
        |> Array.chunkBySize 37
        |> Array.map (fun s -> fst <| MakePartialRecord<Skill> s [|24|])
        |> Array.toList
        |> AllSkills |> update agentId
    | 0x0087us ->
        let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
        ForcedPosition (x0, y0) |> update agentId
        let mutable delay = time() - (int64 <| ToUInt32 data.[2..]) + tickOffset |> float
        Movement {
            Origin = (x0, y0)
            Destination = (x1, y1)
            Delay = if delay < 0.0 then 0.0 else delay
        } |> update agentId
    | 0x0086us ->
        let id = ToUInt32 data.[2..]
        let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
        ForcedPosition (x0, y0) |> update id
        let mutable delay = time() - (int64 <| ToUInt32 data.[12..]) + tickOffset |> float
        Movement {
            Origin = (x0, y0)
            Destination = (x1, y1)
            Delay = if delay < 0.0 then 0.0 else delay
        } |> update id
    //| 0x07fbus -> yield SkillCast (MakeRecord<RawSkillCast> data.[2..]) callback
    //| 0x0977us -> yield UpdateMonsterHP (MakeRecord<MonsterHPInfo> data.[2..])        
    //| 0x008aus -> yield DamageDealt (MakeRecord<RawDamageInfo> data.[2..]) callback
    //| 0x08c8us -> yield DamageDealt2 (MakeRecord<RawDamageInfo2> data.[2..]) callback        
    //| 0x0addus -> yield AddDroppedItem (MakeRecord<ItemDropRaw> data.[2..])
    //| 0x00a1us -> yield RemoveDroppedItem (ToInt32 data.[2..])
    //| 0x00bdus -> yield InitialCharacterStatus (MakeRecord<CharacterStatusRaw> data.[2..])            
    //| 0x099bus -> yield MapProperty (ToInt16 data.[2..]) (ToInt32 data.[4..])
    //| 0x080eus -> yield UpdatePartyMemberHP (ToUInt32 data.[2..]) (ToInt32 data.[6..]) (ToInt32 data.[10..])
    //| 0xa0dus  -> yield AddGear (ParseEquipItem data.[4..])
    | 0x13aus -> () //yield ParameterChange Parameter.AttackRange data.[2..]
    | 0x0088us -> 
        let info = MakeRecord<UnitMove2> data.[2..]
        ForcedPosition (info.X, info.Y) |> update info.Id
    | 0x00b0us ->
        if (data.[2..] |> ToParameter) = Parameter.Speed then
            Speed <| float (ToInt16 data.[4..]) |> update agentId
        //yield ParameterChange (data.[2..] |> ToParameter) data.[4..]
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
  *)
  (*
type MetaResponse =
    | TickOffset of int64
    | Map of string
    | Disconnected of byte
let HandleMetaPacket map update agentId packetType (data: _[]) =
    match packetType with
     | 0x2ebus ->
        let (x, y, _) = UnpackPosition data.[6..]
        ForcedPosition (x, y) |> update agentId
        TickOffset (int64 (ToUInt32 data.[2..])) |> Some
     | 0x0091us ->
        let position = (data.[18..] |> ToInt16,
                        data.[20..] |> ToInt16)
        let map = (let gatFile = ToString data.[..17]
           gatFile.Substring(0, gatFile.Length - 4))
        MapChanged (map, position) |> update agentId
        Map map |> Some
     | 0x007fus -> TickOffset (int64 (ToUInt32 data.[2..])) |> Some        
     | 0x0081us -> Disconnected data.[2] |> Some
     | _ -> None
*)
let rec PacketParser readPacket update agentId time tickOffset map =
    ()
    (*
    let (pType, pData: ReadOnlyMemory<_>) = readPacket()
    let data = pData.ToArray()
    let mutable newMap = map
    let mutable newTickOffset = tickOffset
    let postUpdate = PostUpdate update map
    //let HandleMetaPacket map update agentId packetType (data: _[]) =
    match HandleMetaPacket map postUpdate agentId pType data with
    | Some meta ->
        match meta with
        | Map m -> newMap <- m
        | TickOffset t -> newTickOffset <- t
        //TODO handle disconnection
        | Disconnected r -> Logger.Warn $"Disconnected: {agentId}"
    | None ->
        HandlePacket agentId newTickOffset time postUpdate pType data
    PacketParser readPacket update agentId time newTickOffset newMap
    *)