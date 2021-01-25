module Sunna.JsonLogger

open NLog
open NLog.Targets
open Suave.Sockets
open Suave.WebSocket

       
[<Target("Websocket")>]
type WebsocketTarget() =
    inherit TargetWithLayout()
    static let mutable socket: WebSocket option = None
    
    override this.Write (logEvent: LogEventInfo) =
        let message =
            this.Layout.Render logEvent
            |> System.Text.Encoding.UTF8.GetBytes
            |> ByteSegment
        
        match socket with
        | Some s ->
            Async.Start <| async {
                let! _ = s.send Text message true
                ()
            }
        | None -> ()
        
    static member Socket
        with get() = socket
        and set v = socket <- v
        