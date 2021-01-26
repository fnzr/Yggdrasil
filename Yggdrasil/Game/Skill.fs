namespace Yggdrasil.Game

open Yggdrasil.Types

type Skills =
    {
        List: Skill list
        Points: uint32
    }
    static member Default = {List=List.empty;Points=0u}

    
type SkillCast =
    {
        SkillId: int16
        Property: int
    }
