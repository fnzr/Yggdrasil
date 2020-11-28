using System;
using System.Net;
using Yggdrasil;

namespace PacketParser
{
    class Program
    {

        static void Main(string[] args)
        {
            API.DefaultLogin(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6900),
              "roboco", "111111");
            while (true)
            {
                API.RunCommand(Console.ReadLine());
            }
        }
    }
}