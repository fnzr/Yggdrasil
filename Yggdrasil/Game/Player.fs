namespace Yggdrasil.Game

type Goals =
    {
        Position: (int16 * int16) option
    }
    static member Default = {Position=None}    
