namespace Yggdrasil.Game

module Event = 
    type ConnectionStatus = | Active | Inactive
    type Action = | Idle | Moving | Dead | Casting
    type BehaviorResult = | Success | Failure
    type UnitSpawn = NPC | Player | Monster | Unknown

    type UnitEvent =
        | Action of Action
        | TargetedBySkill

    type GameEvent = interface end
    type PlayerEvent = PlayerEvent of UnitEvent
                        interface GameEvent
                        
    type NonPlayerEvent = UnitEvent of UnitEvent
                            interface GameEvent
                            
    type WorldEvent =
        | ConnectionStatus of ConnectionStatus
        | BehaviorResult of BehaviorResult
        | UnitSpawn of UnitSpawn
        | UnitDespawn
        | MapChanged
        | Ping
        interface GameEvent
