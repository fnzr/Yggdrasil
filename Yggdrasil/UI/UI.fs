module Yggdrasil.UI.UI

open System
open FSharp.Control.Reactive
open Mindmagma.Curses
open Yggdrasil.World.Sensor
open Yggdrasil.World.Message
type CustomLibraryNames() =
    inherit CursesLibraryNames()
    override this.ReplaceLinuxDefaults = true
    override this.NamesLinux = ResizeArray<_>(["libncursesw.so"])

let rec InputReader next = async {
    let input = NCurses.GetChar()
    if input <> -1 then input |> Convert.ToChar |> string |> next
    return! InputReader next
}

let InitUI time player =
    let inputStream = Subject.broadcast
    Async.Start (InputReader inputStream.OnNext)
    let positionStream = PositionStream time player.MessageStream
    let Screen = NCurses.InitScreen()
    NCurses.NoEcho()
    NCurses.Keypad(Screen, true)
    //NCurses.NoDelay(Screen, true)
    NCurses.SetCursor(CursesCursorState.INVISIBLE) |> ignore
    let entityListWindow = NCurses.NewWindow(6, 80, 4, 0)
    let windowStream = Observable.single (WindowType.EntityListWindow entityListWindow)

    let statusWindow = NCurses.NewWindow(3, 80, 0, 0)
    let statusSubscription = StatusWindow.Window statusWindow 80 player.MessageStream positionStream player.Name player.Id

    EntityListWindow.Init entityListWindow
    let entityListSubscription = EntityListWindow.Window windowStream player.Id
                                     player.MessageStream positionStream inputStream
    //NCurses.EndWin()
    ()
