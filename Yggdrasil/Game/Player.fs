namespace Yggdrasil.Game

open NLog
open Yggdrasil.Game.Components
open Yggdrasil.Types
open FSharpPlus.Lens

type Goals() =
    let mutable position: (int * int) option = None
    member this.Logger = LogManager.GetLogger("Goals")
    member this.Position
        with get() = position
        and set v = position <- v
        
type Player =
    {
        Unit: Unit
        Credentials: string * string
        Skills: Skill list
        Inventory: Inventory
        BattleParameters: BattleParameters
        AttributePoints: int16
        Attributes: Attributes
        Level: Level
        Goals: Goals
        SP: int16
        MaxSP: int16
        Dispatch: Command -> unit
    }
    static member Default = {
        Unit = Unit.Default
        Skills = list.Empty
        Credentials = "", ""
        Inventory = Inventory()
        BattleParameters = BattleParameters.Default
        AttributePoints = 0s
        Attributes = Attributes.Default
        Level = Level()
        Goals = Goals()
        SP = 0s
        MaxSP = 0s
        Dispatch = fun _ -> ()
    }
    member this.Id = this.Unit.Id
    member this.Position = this.Unit.Position
    member this.Name = this.Unit.Name    

module Player =
    let inline _Unit f p = f p.Unit <&> fun x -> {p with Unit = x}
    let inline _Attributes f p = f p.Attributes <&> fun x -> {p with Attributes = x}
    let inline _MaxSP f p = f p.MaxSP <&> fun x -> {p with MaxSP = x}
    let inline _SP f p = f p.SP <&> fun x -> {p with SP = x}
    let inline _BattleParameters f p = f p.BattleParameters <&> fun x -> {p with BattleParameters = x}
    let inline _Position p = _Unit << Unit.__position <| p
    let inline _PrimaryAttributes p = _Attributes << Attributes._Primary <| p
    let inline _BonusAttributes p = _Attributes << Attributes._Bonus <| p
    let inline _AttributesUpgradeCost p = _Attributes << Attributes._UpgradeCost <| p
    let inline _HP p = _Unit << Unit._HP <| p
    let inline _MaxHP p = _Unit << Unit._MaxHP <| p
    let inline _Status p = _Unit << Unit._Status <| p
    
    let withStatus status player = setl _Status status player
    