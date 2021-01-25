module Sunna.Server

open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Sunna.JsonLogger

let ws (webSocket: WebSocket) (_: HttpContext) =
    WebsocketTarget.Socket <- Some webSocket
    socket {
        let mutable loop = true

        while loop do
            let! msg = webSocket.read()
            printfn "Websocket message: %A" msg
            match msg with
            | (Text, data, true) ->
                let str = UTF8.toString data
                let response = sprintf "response to %s" str
                let byteResponse =
                    response
                    |> System.Text.Encoding.ASCII.GetBytes
                    |> ByteSegment
                do! webSocket.send Text byteResponse true

            | (Close, _, _) ->
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
                loop <- false

            | _ -> ()
    }

let StartServer () =
    let app =
        choose [
            path "/ws" >=> handShake ws
            path "/" >=> OK "Yggdrasil client index"
        ]
    startWebServer defaultConfig app