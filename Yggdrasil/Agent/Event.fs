namespace Yggdrasil.Agent

type Connection = | Active | Inactive
type Action = | Idle | Moving
type BehaviorResult = | Success | Failure

type GameEvent =
    | Connection of Connection
    | Action of Action
    | BehaviorResult of BehaviorResult

