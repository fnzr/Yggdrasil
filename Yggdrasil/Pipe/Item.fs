module Yggdrasil.Pipe.Item

open Yggdrasil.Game
open Yggdrasil.Types

let WeightSoftCap value (world: World) =
    world.Player.Inventory.WeightSoftCap <- value
    world, []
    
let RemoveItemDrop id (world: World) =
    {world
      with ItemDrops = List.filter (fun i -> i.Id <> id) world.ItemDrops}, []
    
let AddItemDrop (info: ItemDropRaw) (world: World) =
    {world
     with ItemDrops = {
        Id = info.Id
        NameId = info.NameId
        Identified = info.Identified > 0uy
        Position = (int info.PosX, int info.PosY)
        Amount = info.Amount} :: world.ItemDrops
    }, []