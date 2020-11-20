using System;
using System.Net;
using Microsoft.FSharp.Core;
using Yggdrasil;

namespace PacketParser
{
    class Program
    {

        static Unit LoginSuccess(IPEndPoint ip, Messages.Credentials creds)
        {
            //Console.WriteLine(" Login success!");
            //Console.WriteLine(creds);
            CharacterService.SelectCharacter(ip, creds, 0, FSharpFunc<Messages.SpawnZoneInfo, Unit>.FromConverter(CharacterSelected));
            return null;
        }

        static Unit CharacterSelected(Messages.SpawnZoneInfo zoneInfo)
        {
            Console.WriteLine("Character selected!");
            //Console.WriteLine(zoneInfo);
            ZoneService.Connect(zoneInfo);
            return null;
        }

        static void Main(string[] args)
        {
            var loginServer = new IPEndPoint(IPAddress.Parse("192.168.2.10"), 6900);
            LoginService.Authenticate(loginServer, "roboco", "111111", FSharpFunc<Tuple<IPEndPoint, Messages.Credentials>, Unit>.FromConverter(
                t =>
                 LoginSuccess(t.Item1, t.Item2)
                ));
            Console.ReadKey();
            return;
        }
    }
}