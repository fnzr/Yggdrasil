module Yggdrasil.UI.WindowType

open Mindmagma.Curses

type WindowType =
    | EntityListWindow
    | AttributeWindow

type Window =
    {
        Init: nativeint -> unit
        Type: WindowType
    }
    member this.Reset id =
        NCurses.ClearWindow id
        this.Init id
