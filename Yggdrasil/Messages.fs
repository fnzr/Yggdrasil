module Yggdrasil.Messages

open Yggdrasil.Types

type Command =
    | DoneLoadingMap
    | RequestServerTick of int32
    | RequestMove of RequestMove

type Report =
    | Disconnected
    | ConnectionAccepted of StartData
    | Dispatcher of (Command -> unit)
    | Name of string
    | AccountId of uint32    
    | StatusU32 of Parameter * uint32
    | StatusI32 of Parameter * int
    | StatusU16 of Parameter * uint16
    | StatusI16 of Parameter * int16
    | StatusPair of Parameter * uint16 * int16
    | Status64 of Parameter * int64
    | WeightSoftCap of int
    | NonPlayerSpawn of Unit
    | PlayerSpawn of Unit
    | AddSkill of Skill
    | Print
    
type Mailbox = MailboxProcessor<Report>
    
