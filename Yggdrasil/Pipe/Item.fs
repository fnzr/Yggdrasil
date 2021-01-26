module Yggdrasil.Pipe.Item

open NLog
open Yggdrasil.Game
open Yggdrasil.Types
open FSharpPlus.Lens
open Yggdrasil.Utils
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let WeightSoftCap value (world: Game) =
    Tracer.Send <| setl World._Inventory {world.Inventory with WeightSoftCap = value} world
    
let RemoveItemDrop id (world: Game) =
    Tracer.Send <|
        {world with ItemDrops = List.filter (fun i -> i.Id <> id) world.ItemDrops}
    
let AddItemDrop (info: ItemDropRaw) (world: Game) =
    Tracer.Send <|
    {world
     with ItemDrops = {
        Id = info.Id
        NameId = info.NameId
        Identified = info.Identified > 0uy
        Position = (int info.PosX, int info.PosY)
        Amount = info.Amount} :: world.ItemDrops
    }
    
let AddEquipment items world =
    let i = {world.Inventory with
              Equipment = List.append world.Inventory.Equipment items}
    Tracer.Send {world with Inventory = i}
