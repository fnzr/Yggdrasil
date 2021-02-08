module Yggdrasil.PacketStream.Unit

open System
open NLog
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.IO.Decoder
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

let CreateNonPlayer (raw1: UnitRawPart1) (raw2: UnitRawPart2) map postLocation postHealth =
        let (x, y, _) = UnpackPosition [|raw2.PosPart1; raw2.PosPart2; raw2.PosPart3|]
        let oType = match raw1.ObjectType with
                    | 0x1uy | 0x6uy -> EntityType.NPC
                    | 0x0uy -> EntityType.PC
                    | 0x5uy -> EntityType.Monster
                    | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
                            EntityType.Invalid        
        (0u, Yggdrasil.Pipe.Location.Report.New {
            Id = raw1.AID
            Type = oType
            Map = map
            Coordinates = (x, y)
            Speed = raw1.Speed
        }) |> postLocation
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

let UnitStream startMap locationMailbox postHealth =
    let mutable map = startMap
    Observable.map(fun (pType, (pData: ReadOnlyMemory<_>)) ->
        let mutable skipped = None
        let data = pData.ToArray()
        match pType with
        | 0x0091us ->
            map <- (let gatFile = ToString data.[..17]
               gatFile.Substring(0, gatFile.Length - 4))
        | 0x9feus | 0x9ffus ->
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover [|24|]
            CreateNonPlayer part1 part2 map locationMailbox postHealth
        | 0x9fdus ->
            //WalkingUnit appear. Skip MoveStartTime in the packet
            let (part1, leftover) = MakePartialRecord<UnitRawPart1> data.[4..] [||]    
            let (part2, _) = MakePartialRecord<UnitRawPart2> leftover.[4..] [|24|]
            CreateNonPlayer part1 part2 map locationMailbox postHealth
            ()
        | 0x80eus ->
            let info = MakeRecord<PartyHP> data.[2..]
            ({
                Id = info.AccountId
                MaxHP = info.MaxHP
                HP = info.HP
            }: Yggdrasil.Pipe.Health.Health)
            |> Yggdrasil.Pipe.Health.HealthUpdate.Update
            |> postHealth
        | _ -> skipped <- Some pType
        skipped
    )

