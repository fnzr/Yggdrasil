module Yggdrasil.Pipe.Item

open Yggdrasil.Game
open Yggdrasil.Types
open FSharpPlus.Lens
let WeightSoftCap value (world: World) =
    let i = {world.Player.Inventory with WeightSoftCap = value}
    setl World._Player
        {world.Player with Inventory = i}
    <| world
    
let RemoveItemDrop id (world: World) =
    {world
      with ItemDrops = List.filter (fun i -> i.Id <> id) world.ItemDrops}
    
let AddItemDrop (info: ItemDropRaw) (world: World) =
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
    setl World._Player
    <| setl Player._Inventory i world.Player
    <| world