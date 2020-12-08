module Yggdrasil.AgentMailbox

open NLog
open Yggdrasil.Messages
open Yggdrasil.Navigation
open Yggdrasil.Scheduling
open Yggdrasil.Types
let Logger = LogManager.GetCurrentClassLogger()

let EmptyMailbox = MailboxProcessor.Start(fun _ -> async {()})

type AgentState =
    {
        mutable Dispatch: (Command -> unit)
        mutable Scheduler: int64 -> Report -> unit
        mutable Skills: Skill list
        mutable PosX: int
        mutable PosY: int
        mutable Mailbox: Mailbox
        mutable TickOffset: int64
        mutable Parameters: Parameters
        mutable WalkPath: (int * int) list
        mutable Map: string
        mutable Name: string
    }
    static member Default = {
        Dispatch = fun _ -> Logger.Error("Called dispatch but there is none!")
        Scheduler = fun _ _ -> Logger.Error("Called scheduler but there is none!")
        Skills = List.empty
        PosX = 0
        PosY = 0
        WalkPath = List.empty
        Mailbox = EmptyMailbox
        TickOffset = 0L
        Parameters = Parameters.Default
        Map = ""
        Name = ""
    }
    
let OnU32ParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.Weight -> parameters.Weight <- value
    | Parameter.MaxWeight -> parameters.MaxWeight <- value
    | Parameter.SkillPoints -> parameters.SkillPoints <- value
    | Parameter.JobLevel -> parameters.JobLevel <- value
    | Parameter.BaseLevel -> parameters.BaseLevel <- value
    | Parameter.MaxHP -> parameters.MaxHP <- value
    | Parameter.MaxSP -> parameters.MaxSP <- value
    | Parameter.SP -> parameters.SP <- value
    | Parameter.HP -> parameters.HP <- value
    | _ -> ()
    
let OnI16ParameterUpdate code value (parameters: Parameters) =
    match code with
    //| Parameter.Manner -> parameters.Manner <- value
    | Parameter.Hit -> parameters.Hit <- value
    | Parameter.Flee1 -> parameters.Flee1 <- value
    | Parameter.Flee2 -> parameters.Flee2 <- value
    | Parameter.Critical -> parameters.Critical <- value
    | _ -> ()
    
let OnU16ParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.Speed -> parameters.Speed <- value
    | Parameter.AttackSpeed -> parameters.AttackSpeed <- value
    | Parameter.Attack1 -> parameters.Attack1 <- value
    | Parameter.Attack2 -> parameters.Attack2 <- value
    | Parameter.Defense1 -> parameters.Defense1 <- value
    | Parameter.Defense2 -> parameters.Defense2 <- value
    | Parameter.MagicAttack1 -> parameters.MagicAttack1 <- value
    | Parameter.MagicAttack2 -> parameters.MagicAttack2 <- value
    | Parameter.MagicDefense1 -> parameters.MagicDefense1 <- value
    | Parameter.MagicDefense2 -> parameters.MagicDefense2 <- value
    | Parameter.AttackRange -> parameters.AttackRange <- value
    | _ -> ()
    
let OnI32ParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.Zeny -> parameters.Zeny <- value
    | Parameter.USTR -> parameters.STRUpgradeCost <- value
    | Parameter.UAGI -> parameters.AGIUpgradeCost <- value
    | Parameter.UDEX -> parameters.DEXUpgradeCost <- value
    | Parameter.UVIT -> parameters.VITUpgradeCost <- value
    | Parameter.ULUK -> parameters.LUKUpgradeCost <- value
    | Parameter.UINT -> parameters.INTUpgradeCost <- value
    | _ -> ()

let On64ParameterUpdate code value parameters =
    match code with
    | Parameter.BaseExp -> parameters.BaseExp <- value
    | Parameter.JobExp -> parameters.JobExp <- value
    | Parameter.NextBaseExp -> parameters.NextBaseExp <- value
    | Parameter.NextJobExp -> parameters.NextJobExp <- value
    | _ -> ()
    
let OnPairParameterUpdate code value (parameters: Parameters) =
    match code with
    | Parameter.STR -> parameters.STRRaw <- value
    | Parameter.AGI -> parameters.AGIRaw <- value
    | Parameter.DEX -> parameters.DEXRaw <- value
    | Parameter.VIT -> parameters.VITRaw <- value
    | Parameter.LUK -> parameters.LUKRaw <- value
    | Parameter.INT -> parameters.INTRaw <- value
    | _ -> ()

let MailboxFactory () =
    MailboxProcessor.Start(
        fun (inbox:  Mailbox) ->
            let rec loop state =  async {
                let! msg = inbox.Receive()
                match msg with
                | Dispatcher d -> state.Dispatch <- d
                | Scheduler s -> state.Scheduler <- s
                | Mailbox mailbox -> state.Mailbox <- mailbox                
                | Command c -> state.Dispatch c
                | Map m -> state.Map <- m
                | CharacterName n -> state.Name <- n
                | AddSkill s -> state.Skills <- List.append [s] state.Skills
                | StatusU16 (p, v) -> OnU16ParameterUpdate p v state.Parameters
                | Status64 (p, v) -> On64ParameterUpdate p v state.Parameters
                | StatusI32 (p, v) -> OnI32ParameterUpdate p v state.Parameters
                | StatusI16 (p, v) -> OnI16ParameterUpdate p v state.Parameters
                | StatusU32 (p, v) -> OnU32ParameterUpdate p v state.Parameters
                | StatusPair (p, v) -> OnPairParameterUpdate p v state.Parameters
                | NonPlayerSpawn u | PlayerSpawn u -> ()
                | ConnectionAccepted s ->
                    state.TickOffset <- s.StartTime - GetCurrentTick()
                    state.PosX <- s.X; state.PosY <- s.Y
                    Logger.Info("Starting position: ({X}, {Y})", state.PosX, state.PosY)
                    state.Dispatch Command.DoneLoadingMap
                    //state.Dispatch <| Command.RequestServerTick                
                | ServerTick t -> state.TickOffset <- t - GetCurrentTick()
                | SelfIsWalking d ->
                    state.PosX <- d.StartX; state.PosY <- d.StartY
                    //TODO when does server sends map name?
                    state.WalkPath <- Pathfinding.AStar Maps.Maps.["maps/prontera.fld2"]
                                          (state.PosX, state.PosY) (d.EndX, d.EndY)
                    //TODO calculate speed 
                    state.Scheduler (d.StartTime - state.TickOffset + 200L) PerformStep
                | Print -> Logger.Info("{state:A}", state)                
                | PerformStep ->                    
                    match state.WalkPath with
                    | (x, y) :: path ->
                       state.PosX <- x; state.PosY <- y; state.WalkPath <- path
                       //TODO calculate speed
                       state.Scheduler (GetCurrentTick() + 200L) PerformStep
                    | [] -> ()
                | e -> ()//Logger.Info("Received report {id:A}", e)
                return! loop state
            }
            loop AgentState.Default
    )
    
let OnMailboxError (e) = raise e