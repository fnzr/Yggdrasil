module Yggdrasil.Navigation.Maps

open System
open System.Collections.Generic
open System.IO
open NLog
open Yggdrasil.Utils
let Logger = LogManager.GetLogger("Navigation")

[<Flags>]
type CellType =
    | NO_WALK = 0
    | WALK = 1
    | SNIPE = 2
    | WATER = 4
    | CLIFF = 8

type MapData = {
    Height: uint16
    Width: uint16
    Cells: CellType[]
}

let ReadMap (bytes: byte[]) =
    {
        Width = BitConverter.ToUInt16 (bytes, 0)
        Height = BitConverter.ToUInt16 (bytes.[2..], 0)
        Cells = Array.map (fun c -> Enum.Parse(typeof<CellType>, c.ToString()) :?> CellType) bytes.[4..]
    }
    
let Maps = Dictionary<string, MapData>()

let LoadMap name =    
    let filename = sprintf "maps/%s.fld2" name
    Maps.[name] <-
        if File.Exists filename then
            Logger.Debug ("Loading map {mapName}", name)
            let data = File.ReadAllBytes (filename)
            ReadMap data
        else
            Logger.Error ("Map file not found: {filename}", filename)
            {Width=0us; Height = 0us; Cells=[||]}
    

let GetMapData name =
    if not <| Maps.ContainsKey name then LoadMap name
    Maps.[name]
    
    