using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            server.start(ipAddress, 80, 100, "htdocs");
        }

    }
}
