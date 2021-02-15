module Yggdrasil.Navigation.Maps

open System
open System.Collections.Generic
open System.IO
open NLog
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

[<StructuredFormatDisplay("\"{Name}\"")>]
type Map =
    {
        Name: string
        Data: MapData
    }

let _ReadMap (bytes: byte[]) =
    {
        Width = BitConverter.ToUInt16 (bytes, 0)
        Height = BitConverter.ToUInt16 (bytes, 2)
        Cells = Array.map (fun c -> Enum.Parse(typeof<CellType>, c.ToString()) :?> CellType) bytes.[4..]
    }
    
let _Maps = Dictionary<string, Map>()

let _LoadMap name =    
    let filename = sprintf "maps/%s.fld2" name
    _Maps.[name] <-
        { Name = name
          Data =
            if File.Exists filename then
                Logger.Debug ("Loading map {mapName}", name)
                let data = File.ReadAllBytes (filename)
                _ReadMap data
            else
                Logger.Error ("Map file not found: {filename}", filename)
                {Width=0us; Height = 0us; Cells=[||]}
        }
        
let WalkableMap size =
    {
        Name = "walkable"
        Data = {
            Width = size
            Height = size
            Cells = Array.create (int (size * size)) CellType.WALK
        }
    }
    
    
let GetMap name =
    if not <| _Maps.ContainsKey name then _LoadMap name
    _Maps.[name]
    
    