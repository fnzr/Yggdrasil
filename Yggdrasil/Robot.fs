module Yggdrasil.Robot

open System.Net.Sockets
open Yggdrasil.Messages
open Yggdrasil.Utils

let DisconnectClient(client: TcpClient) =
    client.Close() |> ignore
    client.Dispose() |> ignore

type Robot(username, password, loginServer) =
    (*
    member this.AgentProcessor =
        fun (inbox: MailboxProcessor<Message>) ->
            let rec loop() =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                            | Attribute a ->
                                match a with
                                | Attribute.UserPassword c -> c.Reply(username, password)
                                | Attribute.Name -> ()
                            | AlreadyLoggedIn -> this.Restart()
                            | LoginSuccessful (charServer, credentials) ->
                               
                               //this.Credentials = credentials |> ignore
                               //Unwrap this.LoginServerClient DisconnectClient
                               let charClient = new TcpClient()
                               charClient.Connect charServer
                               this.CharServerClient = Some(charClient) |> ignore
                               CharacterService.Connect (charClient.GetStream()) credentials this.Agent
                            | PacketReadError (packetType, message) ->
                               printf "Failed reading packet %X with message \"%s\". Disconnecting." packetType message
                               this.Exit()
                               
                    return! loop()
                }
            loop()
            
    member public this.Agent = MailboxProcessor.Start(this.AgentProcessor)
    *)
    member this.a = 0
