module Yggdrasil.Agent.Event

type Connection = | Active | Inactive
type Action = | Idle | Moving
type BehaviorResult = | Success | Failure
type UnitSpawn = NPC | Player | Monster | Unknown

type GameEvent =
    | Connection of Connection
    | Action of Action
    | BehaviorResult of BehaviorResult
    | UnitSpawn of UnitSpawn
    | UnitDespawn

