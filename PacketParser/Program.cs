using System;
using System.Net;
using Microsoft.FSharp.Core;
using Yggdrasil;

namespace PacketParser
{
    class Program
    {

        static Unit LoginSuccess(IPEndPoint ip, Structure.Credentials creds)
        {
            //Console.WriteLine(" Login success!");
            //Console.WriteLine(creds);
            Handshake.CharacterService.SelectCharacter(ip, creds, 0, FSharpFunc<Structure.SpawnZoneInfo, Unit>.FromConverter(CharacterSelected));
            return null;
        }

        static Unit CharacterSelected(Structure.SpawnZoneInfo zoneInfo)
        {
            Console.WriteLine("Character selected!");
            //Console.WriteLine(zoneInfo);
            Handshake.ZoneService.Connect(zoneInfo);
            return null;
        }

        static void Main(string[] args)
        {
            var x = new byte[]
            {
                0x6, 0xC6, 0xB0, 0x8E, 0x6, 0x0, 0x0, 0x0, 0x0, 0xC8, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x2, 0x28, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x23, 0xC5, 0x75, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0, 0x0, 0x0, 0x4B, 0x61, 0x66, 0x72, 0x61, 0x20, 0x54, 0x65, 0x6C, 0x65, 0x70, 0x6F, 0x72, 0x74, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x23, 0x70, 0x72, 0x6F, 0x0
            };
            //var m = ZoneHelper.PrepareUnit(x);
            //ZoneHelper.PrepareUnit(x);
            //return;
            //Logging.ConfigureLogger();
            //Console.WriteLine(m);
            var loginServer = new IPEndPoint(IPAddress.Parse("192.168.2.10"), 6900);
            Handshake.LoginService.Authenticate(loginServer, "roboco", "111111", FSharpFunc<Tuple<IPEndPoint, Structure.Credentials>, Unit>.FromConverter(
                t =>
                 LoginSuccess(t.Item1, t.Item2)
                ));
            
            Console.ReadKey();
            return;
        }
    }
}