module Yggdrasil.ZonePacketHandler

open Yggdrasil.Structure

let ParameterChange (agent: Agent) (parameter: uint16) (value:uint32) =
    let attribute = match parameter with
                            | 25us -> CharacterAttribute.MaxWeight
                            | x -> raise (System.ArgumentException (sprintf "Unknown attribute: %d" x))
    agent.Post(AttributeChange (attribute, value))
    
let StatusChange (agent: Agent) (status: uint16) (value:uint32) (plus:uint32) =
    let attribute = match status with
                            | _ -> raise (System.ArgumentException (sprintf "Unknown status %d: %d+%d" status value plus))
    ()