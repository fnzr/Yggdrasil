module Yggdrasil.Robot

type Robot(accountId: uint32) =
    member public this.AccountId = accountId
    
    member val WeightSoftCap = 0 with get, set
    member val WeightHardCap = 0 with get, set
    
    member val ParameterMap = Map.empty with get, set
    member val ParameterLongMap = Map.empty with get, set
    
    //dunno if I want a list here
    member val Units = [] with get, set
    
    member val Skills = [] with get, set