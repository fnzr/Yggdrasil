using System;
using System.Linq;
using System.Net;
using Microsoft.FSharp.Core;
using PacketDotNet;
using SharpPcap;
using Yggdrasil;
using Yggdrasil.IO;

namespace PacketSniffer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string ver = SharpPcap.Version.VersionString;
            /* Print SharpPcap version */
            Console.WriteLine("SharpPcap {0}, Example6.DumpTCP.cs", ver);
            Console.WriteLine();

            /* Retrieve the device list */
            var devices = CaptureDeviceList.Instance;

            /*If no device exists, print error */
            if (devices.Count < 1)
            {
                Console.WriteLine("No device found on this machine");
                return;
            }

            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            int i = 0;

            /* Scan the list printing every entry */
            foreach (var dev in devices)
            {
                /* Description */
                Console.WriteLine("{0}) {1} {2}", i, dev.Name, dev.Description);
                i++;
            }

            Console.WriteLine();
            Console.Write("-- Please choose a device to capture: ");
            //i = int.Parse(Console.ReadLine());

            var device = devices[0];

            //Register our handler function to the 'packet arrival' event
            device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);

            //tcpdump filter to capture only TCP/IP packets
            string filter = "ip and tcp and tcp port 5121";
            device.Filter = filter;

            Console.WriteLine();
            Console.WriteLine
                ("-- The following tcpdump filter will be applied: \"{0}\"",
                filter);
            Console.WriteLine
                ("-- Listening on {0}, hit 'Ctrl-C' to exit...",
                device.Description);

            // Start capture 'INFINTE' number of packets
            device.Capture();

            // Close the pcap device
            // (Note: this line will never be called since
            //  we're capturing infinite number of packets
            device.Close();
        }

        /// <summary>
        /// Prints the time, length, src ip, src port, dst ip and dst port
        /// for each TCP/IP packet received on the network
        /// </summary>
        private static void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            if (tcpPacket != null)
            {
                var ipPacket = (PacketDotNet.IPPacket)tcpPacket.ParentPacket;
                System.Net.IPAddress srcIp = ipPacket.SourceAddress;
                System.Net.IPAddress dstIp = ipPacket.DestinationAddress;
                if (srcIp.Equals(IPAddress.Parse("192.168.2.10")))
                {
                    var queue = _mapToClientQueue.Concat(tcpPacket.PayloadData).ToArray();
                    //_mapToClientQueue = Stream.Reader(queue, _mapToClientCallback);
                }
                else
                {
                    var queue = _clientToMapQueue.Concat(tcpPacket.PayloadData).ToArray();
                    //_clientToMapQueue = Stream.Reader(queue, _clientToMapCallback);
                }
            }
        }

        static readonly FSharpFunc<ushort, FSharpFunc<byte[], Unit>> MapToClientCallback =
            CallbackConverter(MapToClientCallbackNative);
        
        static readonly FSharpFunc<ushort, FSharpFunc<byte[], Unit>> ClientToMapCallback =
            CallbackConverter(ClientToMapCallbackNative);

        static FSharpFunc<ushort, FSharpFunc<byte[], Unit>> CallbackConverter(Func<ushort, byte[], Unit> callback)
        {
            return FSharpFunc<ushort, FSharpFunc<byte[], Unit>>.FromConverter(packetType =>
            {
                return FSharpFunc<byte[], Unit>.FromConverter(data => callback(packetType, data));
            });
        }

        private static byte[] _mapToClientQueue = new byte[] {};
        private static byte[] _clientToMapQueue = new byte[] {};
        
        private static FSharpFunc<byte[], Unit> _blankWriter = FSharpFunc<byte[], Unit>.FromConverter(_ => null);
        


        private static Unit MapToClientCallbackNative(ushort packetType, byte[] data)
        {
            Console.Write("Map => Client: ");
            Console.WriteLine($"{packetType:X}");
            return null;
        }
        
        private static Unit ClientToMapCallbackNative(ushort packetType, byte[] data)
        {
            Console.Write("Client => Map: ");
            Console.WriteLine($"{packetType:X}");
            return null;
        }

    }
}