module Yggdrasil.Mailbox

open System.Timers
open NLog
open Yggdrasil.Behavior
open Yggdrasil.Behavior.StateMachine
open Yggdrasil.Types
open Yggdrasil.Agent
let Logger = LogManager.GetLogger("Mailbox")
(*
let MailboxFactory name map dispatcher machineState =    
    MailboxProcessor.Start(
        let MachineTick = Tick Machines.TransitionsMap
        fun inbox ->
            let rec loop agent = async {
                let! msg = inbox.Receive()
                //ProcessMessage agent msg
                //let newMachine = MachineTick machine agent
                return! loop agent
            }
            let timer = new Timer(500.0)
            //timer.Elapsed.Add(Ping inbox)
            timer.Enabled <- true
            timer.AutoReset <- true
            let activeState = ActiveMachineState<Agent>.Create machineState
            let initialAgent = Agent(name, map, inbox, dispatcher, activeState)
            
            //let initialTreeState = behaviorTree.Start initialAgent 
            loop initialAgent //initialTreeState
    )
    

*)