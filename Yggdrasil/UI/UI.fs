module Yggdrasil.UI.UI

open System
open FSharp.Control.Reactive
open Mindmagma.Curses
open Yggdrasil.UI.WindowType
open Yggdrasil.World.Types
open Yggdrasil.World.Stream
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
    let positionStream = PositionStream time player.MessageStream
    let initScreen = NCurses.InitScreen()
    //Necessary for GetChar to work without clearing the screen
    //https://stackoverflow.com/questions/19748685/curses-library-why-does-getch-clear-my-screen
    NCurses.Refresh() |> ignore
    let rows = 8
    let cols = 80
    let mainWindow = NCurses.NewWindow(rows, cols, 4, 0)
    NCurses.NoDelay(initScreen, true)
    NCurses.Keypad(initScreen, true)
    NCurses.NoEcho()
    NCurses.SetCursor(CursesCursorState.INVISIBLE) |> ignore
    let entityListWindow = {
        Init = EntityListWindow.Init
        Type = EntityListWindow
    }
    let attributeWindow = {
        Init = AttributeWindow.Init
        Type = AttributeWindow
    }
    let windowStream =
        Observable.scanInit
        <| entityListWindow
        <| (fun window input ->
                let f1 = 49 |> Convert.ToChar |> string
                let f2 = 50 |> Convert.ToChar |> string
                let optWindow =
                    match input with
                    | _ when f1 = input -> Some entityListWindow
                    | _ when f2 = input -> Some attributeWindow
                    | _ -> None
                match optWindow with
                | Some win -> win.Reset mainWindow; win
                | None -> window)
        <| Observable.startWith ["1"] inputStream
    let statusWindow = NCurses.NewWindow(3, 80, 0, 0)
    let statusSubscription = StatusWindow.Window statusWindow cols player.MessageStream positionStream player.Name player.Id
    let entityListSubscription = EntityListWindow.Window mainWindow windowStream rows player.Id
                                     player.MessageStream positionStream inputStream
    let attributesSubscription = AttributeWindow.Window mainWindow windowStream player.MessageStream
    Async.Start (InputReader inputStream.OnNext)
