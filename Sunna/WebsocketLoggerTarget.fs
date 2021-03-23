module Sunna.JsonLogger

open System
open System.Net.Sockets
open NLog
open NLog.Targets

[<Target("Websocket")>]
type WebsocketTarget() =
    inherit TargetWithLayout()
    let tcpClient: TcpClient = new TcpClient()

    override this.Write (logEvent: LogEventInfo) =
        let message =
            this.Layout.Render logEvent
            |> System.Text.Encoding.UTF8.GetBytes
            |> ReadOnlyMemory

        if not tcpClient.Connected then
            tcpClient.Connect("127.0.0.1", 9999)
        tcpClient.GetStream().WriteAsync message |> ignore
