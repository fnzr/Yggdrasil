namespace Yggdrasil.Game

open System
open System.Diagnostics
open FSharpPlus.Lens
open Yggdrasil
open Yggdrasil.Game.Components
open Yggdrasil.Types

type LootUpdate =
    | NewLoot of ItemDropRaw
    | LootDisappear of int
    
type SettingsUpdate =
    | WeightSoftCap of int
    
type GameStatus = {
    WeightSoftCap: int
    TickOffset: int64
    PlayerId: uint32
}

type UnitUpdate =
    | NewUnit of Unit
    | DelUnit of uint32 * DisappearReason
    
type SkillUpdate =
    | NewSkills of Skill list

type GameUpdate =
    | LootUpdate of LootUpdate
    | SkillUpdate of SkillUpdate
    | UnitUpdate of UnitUpdate
    | UnitSpeed of Id * int16
    | UnitPosition of Id * Position
    | UnitMovement of Id * UnitMove
    | IsConnected of bool
    | TickOffset of int64
    | WeightSoftCap of int
    | MapChanged of string

type GameO = {
    GameUpdate: IObservable<GameUpdate>
    Request: Request -> unit
    PlayerId: Id
    PlayerName: string
}

module Connection =
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let Tick () = stopwatch.ElapsedMilliseconds
    
type Game =
    {
        IsConnected: bool
        IsMapReady: bool
        Credentials: string * string
        Inventory: Inventory
        Gear: Equipment list
        BattleParameters: BattleParameters
        Attributes: Attributes
        Level: Level
        Skills: Skills
        Map: string
        DroppedItems: DroppedItem list
        Units: Map<uint32, Unit>
        TickOffset: int64
        PlayerId: uint32
        Goals: Goals
        Request: Request -> unit
        Inbox: (Game -> Game) -> unit
        Login: Game -> unit
    }
    static member Default = {
        IsConnected = false
        IsMapReady = false
        Credentials = ("","")
        PlayerId = 0u
        Map = ""
        Gear = List.empty
        Inventory = Inventory.Default
        BattleParameters = BattleParameters.Default
        Attributes = Attributes.Default
        Level = Level.Default
        Skills = Skills.Default     
        DroppedItems = list.Empty
        Units = Map.empty
        TickOffset = 0L
        Goals = Goals.Default
        Request = fun _ -> invalidOp "Request function not set"
        Inbox = fun _ -> invalidOp "Inbox not set"
        Login = fun _ -> invalidOp "Login function not set"
    }
    member this.Player = this.Units.[this.PlayerId]
    member this.UpdateUnit (unit: Unit) =
        {this with Units = this.Units.Add(unit.Id, unit)}

module Game =
    
    let inline _Unit f p = f p.Units <&> fun (x: Unit) -> { p with Units = p.Units.Add(x.Id, x)}
    let inline _Inventory f p = f p.Inventory <&> fun x -> {p with Inventory = x}
    let inline _Goals f p = f p.Goals <&> fun x -> {p with Goals = x}
    let inline _Attributes f p = f p.Attributes <&> fun x -> {p with Attributes = x}
    let inline _PrimaryAttributes p = _Attributes << Attributes._Primary <| p
    let inline _Level f p = f p.Level <&> fun x -> {p with Level = x}
    let inline _Zeny p = _Inventory << Inventory._Zeny <| p
            
    let Ping game = Utils.Delay (fun () -> game.Inbox id)
    let UpdateUnit (unit: Unit) (game: Game) = game.UpdateUnit unit
    
    let PlayerIsDead (game: Game) =
        match game.Player.Action with
        | Dead -> true
        | _ -> false
        
    let PlayerIsIdle (game: Game) =
        match game.Player.Action with
        | Idle -> true
        | _ -> false
