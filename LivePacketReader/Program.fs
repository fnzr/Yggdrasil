// Learn more about F# at http://fsharp.org

open System.Collections.Concurrent
open System.Net
open NLog
open PacketDotNet
open SharpPcap
open Yggdrasil.Game
let Logger = LogManager.GetLogger("LivePacket")
let ServerIP = IPAddress.Parse "192.168.2.10"
let ClientIP = IPAddress.Parse "192.168.2.3"

let BlankMailbox = MailboxProcessor.Start(fun inbox ->
    let rec loop () = async {
        let! _ = inbox.Receive()
        return! loop ()
    }
    loop())

Game.World.Player.Id <- 2000001u
let MapToClientCallback = Yggdrasil.IO.Incoming.OnPacketReceived Game
//let mutable MapToClientQueue = Array.empty
//let mutable ClientToMapQueue = Array.empty

let OnClientToServerPacket (packetType: uint16) packetData =
    match packetType with
    | 0x0087us -> () //ZC_NOTIFY_PLAYERMOVE
    | 0x0360us -> () //CZ_REQUEST_TIME2
    | 0x007dus -> () //CZ_NOTIFY_ACTORINIT
    | 0x08c9us -> () //cash shop request?
    | 0x014fus -> () //CZ_REQ_GUILD_MENU
    | 0x0447us -> () //CZ_BLOCKING_PLAY_CANCEL
    | 0x0368us -> () //CZ_REQNAME2
    | 0x035fus -> () //CZ_REQUEST_MOVE2
    | 0x0436us -> () //CZ_ENTER2
    | 0x0361us-> () //CZ_CHANGE_DIRECTION2
    | _ -> Logger.Info ("Packet: {packetType:X}", packetType)

let CSLock = obj()
let SCLock = obj()
let MapToClientQueue = ConcurrentQueue<byte []>()
let ClientToMapQueue = ConcurrentQueue<byte []>()
let OnPacketArrival (e: CaptureEventArgs) =
    match Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data).Extract<TcpPacket>() with
    | null -> ()
    | tcpPacket ->
        let ipPacket = tcpPacket.ParentPacket :?> IPPacket
        match ipPacket.SourceAddress with
        | ip when ServerIP = ip && ipPacket.DestinationAddress = ClientIP ->
            MapToClientQueue.Enqueue tcpPacket.PayloadData
        | ip when ClientIP = ip ->
            ClientToMapQueue.Enqueue tcpPacket.PayloadData
        | _ -> ()
    ()
    
let rec ReadQueue (queue: ConcurrentQueue<byte []>) callback stream = async {
    let (success, bytes) = queue.TryDequeue()
    let next =
        if success then
            let q = Array.concat [| stream; bytes |]
            Yggdrasil.IO.Stream.Reader q callback
        else Async.Sleep 100 |> ignore; stream
    Async.Start <| ReadQueue queue callback next
}

[<EntryPoint>]
let main argv =
    let devices = CaptureDeviceList.Instance;
    if devices.Count < 1 then invalidArg "devices" "No device found in this machine. Did you run as root?"
    //Seq.iteri (fun i (d: ICaptureDevice) -> printfn "%i) %s" i d.Name) (Seq.cast devices)    
    Async.Start <| ReadQueue MapToClientQueue MapToClientCallback Array.empty
    Async.Start <| ReadQueue ClientToMapQueue OnClientToServerPacket Array.empty
    let device = Seq.find (fun (d: ICaptureDevice) -> d.Name = "enp3s0") devices
    device.OnPacketArrival.Add(OnPacketArrival)
    device.Open(DeviceMode.Promiscuous, 1000)

    let filter = "ip and tcp and tcp port 5121";
    device.Filter <- filter;
    printfn "Listening on [%s] with filter \"%s\"" device.Name filter
    device.Capture()
    device.Close();
    0 // return an integer exit code
