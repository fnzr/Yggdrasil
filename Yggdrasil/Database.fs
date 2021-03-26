module Yggdrasil.Database
open System.Text.RegularExpressions
open Farango
open Microsoft.FSharpLu.Json
open Farango.Connection
open Farango.Documents
open Newtonsoft.Json

//url ="http://root:root@172.21.0.2:8529/ragnarok"
let Connect url =
    match Async.RunSynchronously <| connect url with
    | Ok r -> r
    | Error e -> invalidArg url e

type Job =
    {
        [<JsonProperty("_key")>]
        Id: string
        Name: string
    }

type Equipment =
    {
        [<JsonProperty("_key")>]
        Id: string
        Name: string
        Type: string
        SubType: string
        Weight: int
        Attack: int
        MagicAttack: int
        Defense: int
        Range: int
        CardSlots: int
        Gender: string
        WeaponLevel: int
        EquipLevelMin: int
        EquipLevelMax: int
        Refineable: int

    }

let ImportJobs file url = async {
    let conn = Connect url
    let rx = Regex(@"\/\/ (.+)\n(\d+)", RegexOptions.Multiline)
    let content = System.IO.File.ReadAllText(file)
    let matches = rx.Matches(content)
    return!
        Seq.map
        <| fun (m: Match) -> {Id = m.Groups.[2].Value; Name=m.Groups.[1].Value.Replace(" ", "")}
        <| matches
        |> Default.serialize
        |> createDocuments conn "jobs"
}
