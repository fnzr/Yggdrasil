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
            //Logging.ConfigureLogger();
            var loginServer = new IPEndPoint(IPAddress.Parse("10.20.11.41"), 6900);
            Handshake.LoginService.Authenticate(loginServer, "roboco", "111111", FSharpFunc<Tuple<IPEndPoint, Structure.Credentials>, Unit>.FromConverter(
                t =>
                 LoginSuccess(t.Item1, t.Item2)
                ));
            Console.ReadKey();
            return;
        }
    }
}