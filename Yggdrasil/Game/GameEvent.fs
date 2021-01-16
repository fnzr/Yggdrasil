namespace Yggdrasil.Game

module Event = 
    type ConnectionStatus = | Active | Inactive
    type Action = | Idle | Moving | Dead | Casting
    type BehaviorResult = | Success | Failure
    //type UnitSpawn = NPC | Player | Monster | Unknown
    type Health = Increased | Decreased 
    type UnitEvent =
        | Action of Action
        | Health of Health
        | TargetedBySkill
        | DealtDamage

    type GameEvent = interface end
    type PlayerEvent = PlayerEvent of UnitEvent
                        interface GameEvent
                        
    type NonPlayerEvent = UnitEvent of UnitEvent
                            interface GameEvent
                            
    type WorldEvent =
        | ConnectionStatus of ConnectionStatus
        | BehaviorResult of BehaviorResult
        //| UnitSpawn of UnitSpawn
        | UnitDespawn
        | MapChanged
        | ItemDropped
        | ItemDroppedDisappeared
        | Ping
        interface GameEvent
