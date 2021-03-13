
open System.Diagnostics
open System.IO
open System.Net
open FSharp.Control.Reactive
open NLog
open PacketDotNet
open SharpPcap
let Logger = LogManager.GetLogger("LivePacket")
let ServerIP = IPAddress.Parse "192.168.2.10"
let ClientIP = IPAddress.Parse "192.168.2.3"

let OnPacketArrival clientPacket serverPacket (e: CaptureEventArgs) =
    match Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data).Extract<TcpPacket>() with
    | null -> ()
    | tcpPacket ->
        let ipPacket = tcpPacket.ParentPacket :?> IPPacket
        match ipPacket.SourceAddress with
        | ip when ServerIP = ip && ipPacket.DestinationAddress = ClientIP ->
            serverPacket tcpPacket.PayloadData
        | ip when ClientIP = ip ->
            clientPacket tcpPacket.PayloadData
        | _ -> ()
    
let PacketObserver entryPoint buffer =
    Observable.scanInit
    <| (None, new MemoryStream())
    <| fun (_, stream) bytes ->
        stream.Write (bytes, 0, bytes.Length)
        stream.Position <- 0L
        match Yggdrasil.IO.Stream.ReadPacket stream buffer with
        | None -> None, stream
        | Some (pType, pData) ->
            let next = new MemoryStream(int stream.Length - pData.Length)
            stream.CopyTo(next)
            stream.Dispose()
            next.Position <- 0L
            Some (pType, pData), next
    <| entryPoint
    |> Observable.filter (fun (optPacket, _) -> Option.isSome optPacket)
    |> Observable.map (fun (p, _) -> p.Value) 
    |> Observable.publish
    
let ServerToClientObserver time subject =
    Yggdrasil.IO.Incoming.Observer.CreatePlayer
    <| {Id = 2000001u
        Name = "LiveReader"
        Map = "prontera"
        Request = fun _ -> ()
        PacketSource = PacketObserver subject (Array.zeroCreate 1024)
        }
    <| time
    
let ClientToServerObserver subject =
    let obs =  PacketObserver subject (Array.zeroCreate 1024)
    let subscription = 
        Observable.filter
            <| fun (packetType, _) ->
                match packetType with
                | 0x0087us //ZC_NOTIFY_PLAYERMOVE
                | 0x0360us //CZ_REQUEST_TIME2
                | 0x007dus //CZ_NOTIFY_ACTORINIT
                | 0x08c9us //cash shop request?
                | 0x014fus //CZ_REQ_GUILD_MENU
                | 0x0447us //CZ_BLOCKING_PLAY_CANCEL
                | 0x0368us //CZ_REQNAME2
                | 0x035fus //CZ_REQUEST_MOVE2
                | 0x0436us //CZ_ENTER2
                | 0x0361us //CZ_CHANGE_DIRECTION2
                    -> false
                | _ -> true
            <| obs
        |> Observable.subscribe (fun (t, _) -> Logger.Info ("Client Packet: {packetType:X}", t))
    [obs.Connect(); subscription]
    
let StartPacketListener onPacketArrival =
    let devices = CaptureDeviceList.Instance;
    if devices.Count < 1 then invalidArg "devices" "No device found in this machine. Did you run as root?"
    //Seq.iteri (fun i (d: ICaptureDevice) -> printfn "%i) %s" i d.Name) (Seq.cast devices)
    
    let device = Seq.find (fun (d: ICaptureDevice) -> d.Name = "enp3s0") devices
    device.OnPacketArrival.Add(onPacketArrival)
    device.Open(DeviceMode.Promiscuous, 1000)

    let filter = "ip and tcp and tcp port 5121";
    device.Filter <- filter;
    printfn "Listening on [%s] with filter \"%s\"" device.Name filter
    device.Capture()
    device.Close()

[<EntryPoint>]
let main _ =
    let mapToClientSubject = Subject.broadcast
    let clientToMapSubject = Subject.broadcast
    let clock = Stopwatch()
    clock.Start()
    let time = fun () -> clock.ElapsedMilliseconds
    
    let _ = ServerToClientObserver time mapToClientSubject
    let _ = ClientToServerObserver clientToMapSubject
    
    let packetArrival = OnPacketArrival mapToClientSubject.OnNext clientToMapSubject.OnNext
    StartPacketListener packetArrival
    0
