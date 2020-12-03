module Yggdrasil.AgentMailbox

open NLog
open Yggdrasil.PacketTypes
open Yggdrasil.Types
let Logger = LogManager.GetCurrentClassLogger()

type TestState =
    {
        mutable Dispatch: (Command -> unit)
        mutable Skills: SkillRaw list
    }
    static member Default = {
        Dispatch = fun _ -> Logger.Error("Called dispatch but there's none!")
        Skills = List.empty
    }

let MailboxFactory () =
    MailboxProcessor.Start(
        fun (inbox:  MailboxProcessor< Report>) ->            
            let rec loop state =  async {
                //Logger.Info "what the fuck"
                let! msg = inbox.Receive()
                match msg with
                | Dispatcher d -> state.Dispatch <- d
                | AddSkill s -> state.Skills <- List.append [s] state.Skills
                | NonPlayerSpawn u | PlayerSpawn u -> Logger.Info("Unit spawn: {unitName}", u.Name)
                | ConnectionAccepted s ->
                    Logger.Info ("{startData:A}", s)
                    state.Dispatch Command.DoneLoadingMap
                    state.Dispatch <| Command.RequestServerTick 1;
                //| e -> Logger.Info("Received report {id:A}", e)
                return! loop state
            }            
            loop TestState.Default
    )