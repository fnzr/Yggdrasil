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
            var a = PacketTypes.Message.Print;
            var b = PacketTypes.Message.Print;
                
            var x = PacketTypes.Message.NewStatus64Update(1, 2);
            var y = PacketTypes.Message.NewStatus64Update(3, 4);


            var z = YggrasilTypes.Event.StatusChanged;
            //z.Tag
            Console.WriteLine(x.Tag.CompareTo(y.Tag));
            Console.WriteLine(a.Tag.CompareTo(b.Tag));
            Console.WriteLine(a.Tag.CompareTo(x.Tag));
            /*
            var loginServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6900);
            new Task(() =>
                Handshake.LoginService.Authenticate(loginServer, "roboco", "111111", FSharpFunc<Tuple<IPEndPoint, Handshake.Credentials>, Unit>.FromConverter(
                    t =>
                     LoginSuccess(t.Item1, t.Item2)
                    ))
            ).Start();
            */
            //ReadCommands();
            return;
        }
    }
}