module Yggdrasil.ZoneHelper

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open Microsoft.FSharp.Reflection
open Yggdrasil.PacketTypes
open Yggdrasil.Utils


(*    
let SpawnNonPlayer (agent: AgentMailbox) (data: byte[]) =
    //agent.Post(SpawnNPC (MakeRecord<Unit> data [|24|]))
    ()
    
let SpawnPlayer (agent: AgentMailbox) (data: byte[]) =
    //agent.Post(SpawnPlayer (MakeRecord<Unit> data [|24|]))
    ()

let AddSkill (agent: AgentMailbox) (data: byte[]) =
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            agent.Post(AddSkill (MakeRecord<Skill> data [|24|]))
            ParseSkills bytes.[37..]
    ParseSkills data
    ()

let StartWalk (agent: AgentMailbox) (data: byte[]) =
    //let fields = typeof<MoveData>.GetProperties()
    ()
    //agent.Post(Moving ((StructureConstructor<MoveData> data [|24|])))
    
let PartyMemberHPUpdate (agent: AgentMailbox) (data: byte[]) =
    //let fields = typeof<UpdatePartyMemberHP>.GetProperties()
    ()
    //agent.Post(PartyMemberHP ((StructureConstructor<UpdatePartyMemberHP> data [|24|])))
*)