module Yggdrasil.AgentMailbox

open NLog
open Yggdrasil.Messages
open Yggdrasil.Types
let Logger = LogManager.GetCurrentClassLogger()

type AgentState =
    {
        mutable Dispatch: (Command -> unit)
        mutable Skills: Skill list
        mutable PosX: byte
        mutable PosY: byte
    }
    static member Default = {
        Dispatch = fun _ -> Logger.Error("Called dispatch but there's none!")
        Skills = List.empty
        PosX = 0uy
        PosY = 0uy
    }
    
let mutable oldServerTime = 0u
let mutable oldStopwatchTime = 0u 
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
                    state.PosX <- s.X; state.PosY <- s.Y
                    state.Dispatch Command.DoneLoadingMap
                    state.Dispatch <| Command.RequestServerTick
                | Command c -> state.Dispatch c
                | Print -> Logger.Info("{state:A}", state)
                | ServerTick _ -> ()
                | e -> ()//Logger.Info("Received report {id:A}", e)
                return! loop state
            }            
            loop AgentState.Default
    )
    
let OnMailboxError (e) = raise e