// Learn more about F# at http://fsharp.org

open System
open System.Collections.Generic
open System.Net
open System.Net.Sockets
open System.Text
open System.Text.RegularExpressions

let MessageRegex = Regex @"\[([\w\d]+?)\]\[([\w\d]+?)\]"

let ColorMap = Dictionary<string, ConsoleColor>()
["StateMachine", ConsoleColor.Blue;
 "Dispatcher", ConsoleColor.Magenta
 "Goals", ConsoleColor.Yellow
 "Agent", ConsoleColor.Green;] |> Seq.iter ColorMap.Add


let ConsoleLock = obj()
let PrintMessage message color =
    lock ConsoleLock (fun _ ->
        Console.ForegroundColor <- color
        printf "%s" message
        Console.ResetColor())

let rec ReadIncomingText (socket: Socket) (buffer: byte[]) =
    match socket.Receive buffer with
    | 0 -> PrintMessage "Client disconnected\n" ConsoleColor.DarkRed
    | len ->
        let raw = Encoding.UTF8.GetString buffer.[..len-1]
        let m = MessageRegex.Match raw
        let color = if m.Success then
                        match ColorMap.TryGetValue m.Groups.[2].Value with
                        | true, color -> color
                        | false, _ -> ConsoleColor.White
                    else ConsoleColor.White
        PrintMessage raw color
        ReadIncomingText socket buffer

let rec OnConnectionAccepted (ar: IAsyncResult) =
    let server = ar.AsyncState :?> TcpListener
    use client = server.EndAcceptSocket ar
    server.BeginAcceptTcpClient (AsyncCallback OnConnectionAccepted, server) |> ignore
    ReadIncomingText client <| Array.zeroCreate 2056    

[<EntryPoint>]
let main argv =
    let host = IPAddress.Parse "127.0.0.1"
    let port = 9999
    let server = TcpListener(host, port)
    PrintMessage (sprintf "Listening on %A\n" server.LocalEndpoint) ConsoleColor.White
    server.Start()
    server.BeginAcceptSocket (AsyncCallback OnConnectionAccepted, server) |> ignore
    Console.ReadKey() |> ignore
    0 // return an integer exit code
