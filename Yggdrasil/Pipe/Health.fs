module Yggdrasil.Pipe.Health
open Yggdrasil.Types

type Health = {
    Id: Id
    MaxHP: int
    HP: int
}

type HealthUpdate = 
    | Update of Health
    | LostTrack of Id
