namespace Yggdrasil.Game

open System

module Event = 
    type ConnectionStatus = | Active | Inactive
    type Status = | Idle | Moving | Dead | Casting
    type BehaviorResult = | Success | Failure
    //type UnitSpawn = NPC | Player | Monster | Unknown
    type Health = Increased | Decreased 

    type WorldEvent =
        | ConnectionStatus of ConnectionStatus
        | BehaviorResult of BehaviorResult
        //| UnitSpawn of UnitSpawn
        | UnitDespawn
        | MapChanged
        | ItemDropped
        | ItemDroppedDisappeared
        | Ping
        | PlayerPositionChanged
        | PlayerWalkCanceled
