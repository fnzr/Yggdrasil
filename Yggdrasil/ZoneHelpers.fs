module Yggdrasil.ZoneHelper

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open Microsoft.FSharp.Reflection
open Yggdrasil.PacketTypes
open Yggdrasil.Utils

let PropertiesCache = Map.empty
                            .Add(typeof<Unit>.ToString(), typeof<Unit>.GetProperties())

let MakeRecord<'T> (data: byte[]) (stringSizes: int[]) =
    let queue = Queue<obj>()
    let fields = PropertiesCache.[typeof<'T>.ToString()]
    let rec loop (properties: PropertyInfo[]) (data: byte[]) (stringSizes: int[]) =
        match properties with
        | [||] -> FSharpValue.MakeRecord(typeof<'T>, queue.ToArray()) :?> 'T
        | _ ->
            let property = properties.[0]
            let size, stringsS = if property.PropertyType = typeof<string>
                                    then stringSizes.[0], stringSizes.[1..]
                                    else Marshal.SizeOf(property.PropertyType), stringSizes
            
            let value = if property.PropertyType = typeof<int32> then ToInt32 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint32> then ToUInt32 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<byte> then data.[0] :> obj
                        elif property.PropertyType = typeof<int16> then ToInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<uint16> then ToUInt16 data.[..size-1] :> obj
                        elif property.PropertyType = typeof<string> then (Encoding.UTF8.GetString data.[..size-1]) :> obj
                        else raise (ArgumentException "Unhandled type")
            queue.Enqueue(value);
            loop properties.[1..] data.[size..] stringsS    
    loop fields data stringSizes
    
let SpawnNonPlayer (agent: Messenger) (data: byte[]) =
    //agent.Post(SpawnNPC (MakeRecord<Unit> data [|24|]))
    ()
    
let SpawnPlayer (agent: Messenger) (data: byte[]) =
    //agent.Post(SpawnPlayer (MakeRecord<Unit> data [|24|]))
    ()

let AddSkill (agent: Messenger) (data: byte[]) =
    (*
    let rec ParseSkills (skillBytes: byte[]) =
        match skillBytes with
        | [||] -> ()
        | bytes ->
            agent.Post(AddSkill (MakeRecord<Skill> data [|24|]))
            ParseSkills bytes.[37..]
    ParseSkills data
    *)
    ()

let StartWalk (agent: Messenger) (data: byte[]) =
    //let fields = typeof<MoveData>.GetProperties()
    ()
    //agent.Post(Moving ((StructureConstructor<MoveData> data [|24|])))
    
let PartyMemberHPUpdate (agent: Messenger) (data: byte[]) =
    //let fields = typeof<UpdatePartyMemberHP>.GetProperties()
    ()
    //agent.Post(PartyMemberHP ((StructureConstructor<UpdatePartyMemberHP> data [|24|])))