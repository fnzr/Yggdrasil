module Yggdrasil.Communication

open Yggdrasil.PacketTypes

type AgentUpdate =
    | Name of string
    | AccountId of uint32
    | StatusU32 of Parameter * uint32
    | StatusI32 of Parameter * int
    | StatusU16 of Parameter * uint16
    | StatusI16 of Parameter * int16
    | StatusPair of Parameter * uint16 * int16
    | Status64 of Parameter * int64
    | Print

type AgentMailbox = MailboxProcessor<AgentUpdate>

type AgentEvent =
    | ParameterChanged of Parameter
    | HealthChanged of uint32 * uint32