namespace Yggdrasil.Game

module Components = 
    type Level() =
        member val BaseLevel = 0u with get, set
        member val JobLevel = 0u with get, set
        member val BaseExp = 0L with get, set
        member val JobExp = 0L with get, set
        member val NextBaseExp = 0L with get, set
        member val NextJobExp = 0L with get, set
        member val StatusPoints = 0u with get, set
        member val SkillPoints = 0u with get, set
        
    type Inventory() =
        member val WeightSoftCap = 0 with get, set
        member val Weight = 0u with get, set
        member val MaxWeight = 0u with get, set
        member val Zeny = 0 with get, set
