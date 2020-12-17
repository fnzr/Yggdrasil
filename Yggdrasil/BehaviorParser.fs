module Yggdrasil.BehaviorParser

open System.Collections.Generic
open YamlDotNet.Serialization


let contents = System.IO.File.ReadAllText("behavior/default.yaml")
let deserializer = Deserializer()
let behavior = deserializer.Deserialize<Dictionary<string, List<string>>>(contents)
