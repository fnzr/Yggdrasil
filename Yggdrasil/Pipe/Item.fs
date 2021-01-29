module Yggdrasil.Pipe.Item

open NLog
open Yggdrasil.Game
open Yggdrasil.Types
open FSharpPlus.Lens
open Yggdrasil.Utils
let Tracer = LogManager.GetLogger ("Tracer", typeof<JsonLogger>) :?> JsonLogger

let SetWeightSoftCap value (game: Game) =
    Tracer.Send <| setl Game._Inventory {game.Inventory with WeightSoftCap = value} game
    
let RemoveDroppedItem id (game: Game) =
    Tracer.Send <|
        {game with DroppedItems = List.filter (fun i -> i.Id <> id) game.DroppedItems}
    
let AddDroppedItem (info: ItemDropRaw) (game: Game) =
    Tracer.Send <|
    {game
     with DroppedItems = {
        Id = info.Id
        NameId = info.NameId
        Identified = info.Identified > 0uy
        Position = (int info.PosX, int info.PosY)
        Amount = info.Amount} :: game.DroppedItems
    }
    
let AddGear items game =
    Tracer.Send {game with Gear = List.append game.Gear items}
