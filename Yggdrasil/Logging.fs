module Yggdrasil.Logging

open NLog

let LogPacket =
    let logger = LogManager.GetCurrentClassLogger()
    fun (accountId: uint32) (packetType: uint16) (data: byte[]) ->
        logger.Debug("[{accountId}] Received packet {packetType:X} with length {length}", accountId, packetType, data.Length)
        
let LogBinary =
    let logger = LogManager.GetCurrentClassLogger()
    fun (data: byte[]) ->
        logger.Debug("{data:X}", data)
        
(*let LogMessage =
    let logger = LogManager.GetCurrentClassLogger()
    fun (accountId: uint32) (message: Message) ->
        logger.Debug("Character {accountId} => {message}", accountId, message)
*)