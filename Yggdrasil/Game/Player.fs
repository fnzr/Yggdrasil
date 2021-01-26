namespace Yggdrasil.Game

open NLog
open Yggdrasil.Game.Components
open Yggdrasil.Types
open FSharpPlus.Lens

type Goals =
    {
        Position: (int * int) option
    }
    static member Default = {Position=None}    
