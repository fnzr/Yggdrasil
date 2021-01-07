namespace Yggdrasil.Agent

open NLog
open Yggdrasil.Types

type BattleParameters() =
    inherit EventDispatcher()
    let ev = Event<_>()
    override this.Logger = LogManager.GetLogger "BattleParameters"
    override this.Dispatch e = ev.Trigger e
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    member val STRRaw = 0us, 0s with get, set
    member val AGIRaw = 0us, 0s with get, set
    member val VITRaw = 0us, 0s with get, set
    member val INTRaw = 0us, 0s with get, set
    member val DEXRaw = 0us, 0s with get, set
    member val LUKRaw = 0us, 0s with get, set
    member val AttackRange = 0us with get, set
    member val AttackSpeed = 0us with get, set
    member val Attack1 = 0us with get, set
    member val Attack2 = 0us with get, set
    member val MagicAttack1 = 0us with get, set
    member val MagicAttack2 = 0us with get, set
    member val Defense1 = 0us with get, set
    member val Defense2 = 0us with get, set
    member val MagicDefense1 = 0us with get, set
    member val MagicDefense2 = 0us with get, set
    member val Hit = 0s with get, set
    member val Flee1 = 0s with get, set
    member val Flee2 = 0s with get, set
    member val Critical = 0s with get, set
    member val Speed = 150L with get, set
    member val STRUpgradeCost = 0 with get, set
    member val AGIUpgradeCost = 0 with get, set
    member val VITUpgradeCost = 0 with get, set
    member val INTUpgradeCost = 0 with get, set
    member val DEXUpgradeCost = 0 with get, set
    member val LUKUpgradeCost = 0 with get, set
    
type Level() =
    inherit EventDispatcher()
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger e
    override this.Logger = LogManager.GetLogger "Level"
    member val BaseLevel = 0u with get, set
    member val JobLevel = 0u with get, set
    member val BaseExp = 0L with get, set
    member val JobExp = 0L with get, set
    member val NextBaseExp = 0L with get, set
    member val NextJobExp = 0L with get, set
    member val StatusPoints = 0u with get, set
    member val SkillPoints = 0u with get, set
    
type Health() =
    inherit EventDispatcher()
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger(e)
    override this.Logger = LogManager.GetLogger "HPSP"
    member val HP = 0u with get, set
    member val MaxHP = 0u with get, set
    member val SP = 0u with get, set
    member val MaxSP = 0u with get, set
        
    
type Inventory() =
    inherit EventDispatcher()
    let ev = Event<_>()
    [<CLIEvent>]
    member this.OnEventDispatched = ev.Publish
    override this.Dispatch e = ev.Trigger e
    override this.Logger = LogManager.GetLogger "Inventory"
    member val WeightSoftCap = 0 with get, set
    member val Weight = 0u with get, set
    member val MaxWeight = 0u with get, set
    member val Zeny = 0 with get, set


