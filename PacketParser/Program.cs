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
        private static FSharpMailboxProcessor<Publisher.PublisherMessages> MessageBox = Publisher.CreatePublisher();


        static void Main(string[] args)
        {
            API.Login(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6900),
                "roboco", "111111");
            while (true)
            {
                API.RunCommand(Console.ReadLine());
            }
            var a = PacketTypes.Message.Print;
            var b = PacketTypes.Message.Print;
                
            var x = PacketTypes.Message.NewStatus64Update(PacketTypes.StatusCode.Attack2, 2);
            var y = PacketTypes.Message.NewStatus64Update(PacketTypes.StatusCode.Attack1, 4);


            var z = YggrasilTypes.Event.StatusChanged;
            //z.Tag
            //Console.WriteLine(x.CompareTo(y.Tag));
            //Console.WriteLine(a.CompareTo(b.Tag));

            Console.WriteLine(a.GetType() == b.GetType());
            Console.WriteLine(x.GetType() == y.GetType());
            Console.WriteLine(x.GetType() == a.GetType());
            
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