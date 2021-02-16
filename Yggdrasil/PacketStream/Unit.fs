module Yggdrasil.PacketStream.Unit

open System
open System.Collections.Concurrent
open NLog
open Yggdrasil.Types
open Yggdrasil.IO.Decoder
open Yggdrasil.Pipe.Message
open Yggdrasil.PacketStream.Observer
let Logger = LogManager.GetLogger "UnitStream"

type UnitWalking = {
    ObjectType: byte
    AID: uint32
    GID: uint32
    Speed: int16
    BodyState: int16
    HealthState: int16
    EffectState: int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory: uint16
    MoveStartTime: uint32
    Accessory2: uint16
    Accessory3: uint16
    HeadPalette: int16
    BodyPalette: int16
    HeadDir: int16
    Robe: uint16
    GUID: uint32
    GEmblemVer: int16
    Honor: int16
    Virtue: int
    isPKModeOn: byte
    Sex: byte
    PosDir1: byte
    PosDir2: byte
    PosDir3: byte
    xSize: byte
    ySize: byte
    CLevel: int16
    Font: int16
    MaxHP: int
    HP: int
    isBoss: byte
    Body: int16
    Name: string
}

type UnitIdle = {
    ObjectType: byte
    AID: uint32
    GID: uint32
    Speed: int16
    BodyState: int16
    HealthState: int16
    EffectState: int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory: uint16
    Accessory2: uint16
    Accessory3: uint16
    HeadPalette: int16
    BodyPalette: int16
    HeadDir: int16
    Robe: uint16
    GUID: uint32
    GEmblemVer: int16
    Honor: int16
    Virtue: int
    isPKModeOn: byte
    Sex: byte
    PosDir1: byte
    PosDir2: byte
    PosDir3: byte
    xSize: byte
    ySize: byte
    CLevel: int16
    Font: int16
    MaxHP: int
    HP: int
    isBoss: byte
    Body: int16
    Name: string
}

type UnitRawPart1 = {
    ObjectType: byte
    AID: uint32
    GID: uint32
    Speed: uint16
    BodyState: int16
    HealthState: int16
    EffectState: int
    Job: int16
    Head: uint16
    Weapon: uint32
    Accessory: uint16
}

type UnitRawPart2 = {    
    Accessory2: uint16
    Accessory3: uint16
    HeadPalette: int16
    BodyPalette: int16
    HeadDir: int16
    Robe: uint16
    GUID: uint32
    GEmblemVer: int16
    Honor: int16
    Virtue: int
    isPKModeOn: byte
    Sex: byte
    PosPart1: byte
    PosPart2: byte
    PosPart3: byte
    xSize: byte
    ySize: byte
    State: byte
    CLevel: int16
    Font: int16
    MaxHP: int
    HP: int
    isBoss: byte
    Body: int16
    Name: string
}

let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) (speeds: ConcurrentDictionary<Id, float>) map =
        let (x, y, _) = UnpackPosition [|raw2.PosPart1; raw2.PosPart2; raw2.PosPart3|]
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> EntityType.NPC
                    | 0x0uy -> EntityType.PC
                    | 0x5uy -> EntityType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            EntityType.Invalid
        speeds.[raw1.AID] <- float raw1.Speed
        [
        {
            Id = raw1.AID
            Map = map
            Position = Known (x, y)
        } |> Location;
        {
            Id = raw1.AID
            Type = oType
            Name = raw2.Name.Split("#").[0]
        } |> New;
        ] |> Messages
        (*
        match oType with
        | EntityType.PC | EntityType.Monster ->
            ({
                Id = raw1.AID
                MaxHP = raw2.MaxHP
                HP = raw2.HP
            }: Yggdrasil.Pipe.Health.Health)
            |> Yggdrasil.Pipe.Health.HealthUpdate.Update
            |> postHealth
        | _ -> ()
        *)

let UnitStream playerId startMap tick =
    let mutable map = Yggdrasil.Navigation.Maps.GetMap startMap
    let mutable tickOffset = 0L
    let speeds = ConcurrentDictionary()
    Observable.map(fun (pType, (pData: ReadOnlyMemory<_>)) ->
        let data = pData.ToArray()
        match pType with
        | 0x0091us ->
            map <- Yggdrasil.Navigation.Maps.GetMap
                       (let gatFile = ToString data.[..17]
                    gatFile.Substring(0, gatFile.Length - 4))
            {
                Id = playerId
                Map = map
                Position = Known (data.[18..] |> ToInt16, data.[20..] |> ToInt16)
            } |> Location |> Message
        | 0x9feus | 0x9ffus ->
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
            CreateNonPlayer part1 part2 speeds map
        | 0x9fdus ->
            //WalkingUnit appear. Skip MoveStartTime in the packet
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover.[4..] [|24|]
            CreateNonPlayer part1 part2 speeds map
        | 0x2ebus ->
            let (x, y, _) = UnpackPosition data.[6..]            
            tickOffset <- int64 (ToUInt32 data.[2..])
            {
                Id = playerId
                Map = map
                Position = Known (x, y)
            } |> Location |> Message
        | 0x0088us -> 
            let info = MakeRecord<UnitMove2> data.[2..]
            {
                Id = info.Id
                Map = map
                Position = Known (info.X, info.Y)
            } |> Location |> Message
        | 0x0080us ->
            //TODO handle disappear reason
            let reason = Enum.Parse(typeof<DisappearReason>, string data.[6]) :?> DisappearReason
            {
                Id = ToUInt32 data.[2..]
                Map = map
                Position = Unknown
            } |> Location |> Message
            //(ToUInt32 data.[2..], Disappear reason) |> mailbox
        | 0x0087us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[2..]) + tickOffset |> float
            {
                Id = playerId
                Map = map
                Speed = speeds.[playerId]
                Origin = Known (x0, y0)
                Target = Known (x1, y1)
                Delay = if delay < 0.0 then 0.0 else delay
            } |> Movement |> Message
        | 0x0086us ->
            let (x0, y0, x1, y1, _, _) = UnpackPosition2 data.[6..]
            let mutable delay = tick() - (int64 <| ToUInt32 data.[12..]) + tickOffset |> float
            let id = ToUInt32 data.[2..]
            {
                Id = id
                Map = map
                Speed = speeds.[id]
                Origin = Known (x0, y0)
                Target = Known (x1, y1)
                Delay = if delay < 0.0 then 0.0 else delay
            } |> Movement |> Message
        | 0x00b0us ->            
            if (data.[2..] |> ToParameter) = Parameter.Speed then
                speeds.[playerId] <- data.[4..] |> ToUInt16 |> float
            Skip
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
        | _ -> Unhandled pType
    )

