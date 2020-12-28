module Yggdrasil.Agent

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open NLog
open Yggdrasil.Types
open Yggdrasil.Behavior

type Goals() =
    let Logger = LogManager.GetLogger("Goals")    
    let mutable position: (int * int) option = None
    member this.LogNewValue (value, ([<Optional; DefaultParameterValue(""); CallerMemberName>] name:string)) =
        Logger.Debug("{name}: {value}", name, value)
    member this.LogValueChange (oldV, newV, ([<Optional; DefaultParameterValue(""); CallerMemberName>] name:string)) =
        Logger.Debug("{name}: {oldV} => {newV}", name, oldV, newV)
    member this.Position
        with get() = position
        and set (v: (int * int) option ) = this.LogValueChange (position, v); position <- v


type Agent (name, map, mailbox, dispatcher, machineState) =
    let Logger = LogManager.GetLogger("Agent")
    let mutable destination: (int * int) option = None
    let mutable position = 0, 0
    let mutable map: string = map
    let mutable inventory = Inventory.Default
    let mutable battleParameters = BattleParameters.Default
    let mutable level = Level.Default
    let mutable skills: Skill list = []
    let mutable hpsp = HPSP.Default
    let mutable isConnected = false
    let mutable machineState: StateMachine.ActiveMachineState<Agent> = machineState
    let goals = Goals()    
    member this.LogNewValue (value, ([<Optional; DefaultParameterValue(""); CallerMemberName>] name:string)) =
        Logger.Debug("{name}: {value}", name, value)
    member this.LogValueChange (oldV, newV, ([<Optional; DefaultParameterValue(""); CallerMemberName>] name:string)) =
        Logger.Debug("{name}: {oldV} => {newV}", name, oldV, newV)
    member this.Dispatcher: Command -> unit = dispatcher
    member this.Mailbox: MailboxProcessor<StateMessage> = mailbox
    member this.Name: string = name
    member this.Goals = goals
    member this.MachineState
        with get() = machineState
        and set v = this.LogValueChange (machineState, v); machineState <- v
    member this.Map
        with get() = map
        and set v = this.LogNewValue v; map <- v
    member this.Destination
        with get() = destination
        and set v = match v with
                    | None -> Logger.Debug("Reached position: {position}", position)
                    | _ -> ()
                    destination <- v
                    //this.LogNewValue v; destination <- v
    member this.Position
        with get() = position
        and set v =
            //this.LogNewValue v
            position <- v
    member this.Inventory
        with get() = inventory
        and set v = this.LogNewValue v; inventory <- v
    member this.BattleParameters
        with get() = battleParameters
        and set v = this.LogNewValue v; battleParameters <- v
    member this.Level
        with get() = level
        and set v = this.LogNewValue v; level <- v
    member this.Skills
        with get() = skills
        and set v = this.LogNewValue v; skills <- v
    member this.HPSP
        with get() = hpsp
        and set v = this.LogNewValue v; hpsp <- v    
    member this.IsConnected
        with get() = isConnected
        and set v = this.LogNewValue v; isConnected <- v
