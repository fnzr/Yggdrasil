module Yggdrasil.PacketTypeCodes

open System.Collections.Generic

module AC =
    let PARSE_OPT_LOGIN = 0xAE3us
    let ACCEPT_LOGIN3 = 0xAC4us
    
let EXPLICIT_LENGTH_PACKETS = new HashSet<uint16>([|
    AC.ACCEPT_LOGIN3
    AC.PARSE_OPT_LOGIN
|])