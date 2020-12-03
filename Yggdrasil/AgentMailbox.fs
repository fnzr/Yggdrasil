module Yggdrasil.AgentMailbox

open NLog
open Yggdrasil.Messages
open Yggdrasil.Types
let Logger = LogManager.GetCurrentClassLogger()

type AgentState =
    {
        mutable Dispatch: (Command -> unit)
        mutable Skills: Skill list
        mutable posX: byte
        mutable posY: byte
    }
    static member Default = {
        Dispatch = fun _ -> Logger.Error("Called dispatch but there's none!")
        Skills = List.empty
        posX = 0uy
        posY = 0uy
    }

let MailboxFactory () =
    MailboxProcessor.Start(
        fun (inbox:  Mailbox) ->            
            let rec loop state =  async {
                let! msg = inbox.Receive()
                match msg with
                | Dispatcher d -> state.Dispatch <- d
                | AddSkill s -> state.Skills <- List.append [s] state.Skills
                | NonPlayerSpawn u | PlayerSpawn u -> Logger.Info("Unit spawn: {unitName}", u.Name)
                | ConnectionAccepted s ->
                    Logger.Info ("{startData:A}", s)
                    state.posX <- s.X; state.posY <- s.Y
                    state.Dispatch Command.DoneLoadingMap
                    state.Dispatch <| Command.RequestServerTick 1
                | Command c -> state.Dispatch c
                | e -> Logger.Info("Received report {id:A}", e)
                return! loop state
            }            
            loop AgentState.Default
    )