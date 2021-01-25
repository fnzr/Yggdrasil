module Yggdrasil.Pipe.Item

open NLog
open Yggdrasil.Game
open Yggdrasil.Types
open FSharpPlus.Lens
open Yggdrasil.Utils
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let WeightSoftCap value (world: World) =
    let i = {world.Player.Inventory with WeightSoftCap = value}
    Tracer.Send <|
    setl World._Player
        {world.Player with Inventory = i}
    <| world
    
let RemoveItemDrop id (world: World) =
    Tracer.Send <|
    {world
      with ItemDrops = List.filter (fun i -> i.Id <> id) world.ItemDrops}
    
let AddItemDrop (info: ItemDropRaw) (world: World) =
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
    let i = {world.Player.Inventory with
              Equipment = List.append world.Player.Inventory.Equipment items}
    Tracer.Send
        setl World._Player
        <| setl Player._Inventory i world.Player
        <| world