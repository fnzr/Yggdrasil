using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using NLog;
using Yggdrasil;

namespace PacketParser
{
    class Program
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();
        private static Dictionary<uint, FSharpMailboxProcessor<PacketTypes.Message>> Mailboxes = new Dictionary<uint, FSharpMailboxProcessor<PacketTypes.Message>>();

        static Unit LoginSuccess(IPEndPoint ip, Handshake.Credentials creds)
        {
            Logger.Info("Successful login: {accountId}", creds.AccountId);
            //Console.WriteLine(" Login success!");
            //Console.WriteLine(creds);
            Handshake.CharacterService.SelectCharacter(ip, creds, 0, FSharpFunc<Handshake.SpawnZoneInfo, Unit>.FromConverter(CharacterSelected));
            return null;
        }

        static Unit CharacterSelected(Handshake.SpawnZoneInfo zoneInfo)
        {
            Console.WriteLine("Character selected!");
            //Console.WriteLine(zoneInfo);
            var mailbox = Handshake.ZoneService.Connect(zoneInfo);
            Mailboxes[zoneInfo.AccountId] = mailbox;
            return null;
        }

        static void ReadCommands()
        {
            while (true)
            {
                try
                {
                    var command = Console.ReadLine()?.Split(' ');
                    if (command[0].Equals("exit"))
                    {
                        break;
                    }
                    var messageTypes =
                        FSharpType.GetUnionCases(typeof(PacketTypes.Message), FSharpOption<BindingFlags>.None);
                    var messageCaseInfo = messageTypes.FirstOrDefault(c =>
                    {
                        Console.WriteLine(c.Name);return c.Name.Equals(command?[0]);
                    });
                    if (messageCaseInfo == null)
                    {
                        throw new ArgumentException($"Unknown message: {command[0]}");
                    }

                    if (Mailboxes.TryGetValue(Convert.ToUInt32(command[1]), out var mailbox))
                    {
                        var fields = messageCaseInfo.GetFields();
                        var values = new List<object>();
                        for (var i = 0; i < fields.Length; i++)
                        {
                            var field = fields[i];
                            var value = Convert.ChangeType(command[2 + i], field.PropertyType);
                            values.Add(value);
                        }

                        var message = FSharpValue.MakeUnion(messageCaseInfo, values.ToArray(), FSharpOption<BindingFlags>.None);
                        mailbox.Post(message as PacketTypes.Message);
                    }
                    else
                    {
                        throw new ArgumentException($"Account id not found: {command[1]}");
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
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
            var loginServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6900);
            new Task(() =>
                Handshake.LoginService.Authenticate(loginServer, "roboco", "111111", FSharpFunc<Tuple<IPEndPoint, Handshake.Credentials>, Unit>.FromConverter(
                    t =>
                     LoginSuccess(t.Item1, t.Item2)
                    ))
            ).Start();
            ReadCommands();
            return;
        }
    }
}