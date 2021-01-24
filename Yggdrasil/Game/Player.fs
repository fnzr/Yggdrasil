namespace Yggdrasil.Game

open NLog
open Yggdrasil.Game.Components
open Yggdrasil.Types
open FSharpPlus.Lens

type Goals =
    {
        Position: (int * int) option
    }
    static member Default = {Position=None}    
        
type Player =
    {
        Unit: Unit
        Credentials: string * string
        Skills: Skill list
        SkillPoints: uint32
        Inventory: Inventory
        BattleParameters: BattleParameters
        Attributes: Attributes
        Level: Level
        Goals: Goals
        SP: int16
        MaxSP: int16
    }
    static member Default = {
        Unit = Unit.Default
        Skills = list.Empty
        SkillPoints = 0u
        Credentials = "", ""
        Inventory = Inventory.Default
        BattleParameters = BattleParameters.Default
        Attributes = Attributes.Default
        Level = Level.Default
        Goals = Goals.Default
        SP = 0s
        MaxSP = 0s
    }
    member this.Id = this.Unit.Id
    member this.Position = this.Unit.Position
    member this.Name = this.Unit.Name    

module Player =
    let inline _Unit f p = f p.Unit <&> fun x -> {p with Unit = x}
    let inline _Inventory f p = f p.Inventory <&> fun x -> {p with Inventory = x}
    let inline _Level f p = f p.Level <&> fun x -> {p with Level = x}
    let inline _Attributes f p = f p.Attributes <&> fun x -> {p with Attributes = x}
    let inline _MaxSP f p = f p.MaxSP <&> fun x -> {p with MaxSP = x}
    let inline _SP f p = f p.SP <&> fun x -> {p with SP = x}
    let inline _BattleParameters f p = f p.BattleParameters <&> fun x -> {p with BattleParameters = x}
    let inline _Goals f p = f p.Goals <&> fun x -> {p with Goals = x}
    let inline _Position p = _Unit << Unit.__position <| p
    let inline _PrimaryAttributes p = _Attributes << Attributes._Primary <| p
    let inline _BonusAttributes p = _Attributes << Attributes._Bonus <| p
    let inline _AttributesUpgradeCost p = _Attributes << Attributes._UpgradeCost <| p
    let inline _HP p = _Unit << Unit._HP <| p
    let inline _MaxHP p = _Unit << Unit._MaxHP <| p
    let inline _Status p = _Unit << Unit._Status <| p
    
    let inline _Zeny p = _Inventory << Inventory._Zeny <| p
    let inline _Weight p = _Inventory << Inventory._Weight <| p
    
    let withStatus status player = setl _Status status player
    