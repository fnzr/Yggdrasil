module Yggdrasil.Logging

open NLog
open Yggdrasil.Structure

(*
let ConfigureLogger () = 
    let Console = new NLog.Targets.ConsoleTarget()

    let Config =  NLog.Config.LoggingConfiguration()
    Config.AddRule(LogLevel.Debug, LogLevel.Fatal, Console)

    LogManager.Configuration <- Config
*)
let LogPacket =
    let logger = LogManager.GetCurrentClassLogger()
    fun (accountId: uint32) (packetType: uint16) (data: byte[]) ->
        logger.Debug("Received packet {packetType:X} with length {length}", packetType, data.Length)
        
let LogBinary =
    let logger = LogManager.GetCurrentClassLogger()
    fun (data: byte[]) ->
        logger.Debug("{data:X}", data)
        
let LogMessage =
    let logger = LogManager.GetCurrentClassLogger()
    fun (accountId: uint32) (message: Message) ->
        logger.Info("Character {accountId} => {message}", accountId, message)