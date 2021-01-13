module Yggdrasil.Agent.Unit

open System
open NLog
open Yggdrasil.Types

let Logger = LogManager.GetLogger("Unit")

type ObjectType =
  | NPC
  | Monster
  | Invalid

[<AbstractClass>]
type Unit(aid, name) =
  abstract member AID: uint32
  abstract member Type: ObjectType
  abstract member Name: string
  abstract member FullName: string
  default this.AID = aid
  default this.FullName = name
  default this.Name = this.FullName.Split("#").[0]
  override this.Equals o =
    match o with
    | :? Unit as u -> u.AID = this.AID
    | :? uint32 as aid -> aid = this.AID
    | _ -> false
  override this.GetHashCode () = this.AID.GetHashCode()
  interface IComparable with
    member this.CompareTo o =
      match o with
      | :? Unit as u -> this.AID.CompareTo(u.AID)
      | :? uint32 as aid -> this.AID.CompareTo(aid)
      | _ -> invalidOp "Comparing Unit to object"
  
type NPC (raw1: UnitRawPart1, raw2: UnitRawPart2) =
  inherit Unit(raw1.AID, raw2.Name)
  override this.Type = ObjectType.NPC
  member this.Position = (int raw2.PosX, int raw2.PosY)
  
type Monster (raw1: UnitRawPart1, raw2: UnitRawPart2) =
  inherit Unit(raw1.AID, raw2.Name)
  override this.Type = ObjectType.Monster
  member this.Position = (int raw2.PosX, int raw2.PosY)
  
let CreateMockUnit aid = {new Unit(aid, "Mock") with override this.Type = ObjectType.Invalid}
let CreateUnit (raw1: UnitRawPart1) (raw2: UnitRawPart2) =
  match raw1.ObjectType with
  | 0x1uy | 0x6uy -> NPC(raw1, raw2) :> Unit
  | 0x5uy -> Monster(raw1, raw2) :> Unit
  | t -> Logger.Warn ("Unhandled ObjectType: {type}", t);
          {new Unit(raw1.AID, raw2.Name) with override this.Type = ObjectType.Invalid}

  

(*
] { ObjectType = 6uy
  AID = 110018954u
  GUI = 0u
  Speed = 200s
  BodyState = 0s
  HealthState = 0s
  EffectState = 0
  Job = 701s
  Head = 0us
  Weapon = 0u
  Accessory1 = 0us
  Accessory2 = 0us
  Accessory3 = 0us
  HeadPalette = 0s
  BodyPalette = 0s
  HeadDir = 0s
  Robe = 0us
  GUID = 0u
  GEmblemVer = 0s
  Honor = 0s
  Virtue = 0
  IsPKModeOn = 0uy
  Gender = 0uy
  PosX = 35uy
  PosY = 70uy
  Direction = 20uy
  xSize = 0uy
  State = 0uy
  CLevel = 0s
  Font = 0s
  MaxHP = -256
  HP = -1
  IsBoss = 255uy
  Body = 0us
  Name = "Warmhearted woman" }
*)
