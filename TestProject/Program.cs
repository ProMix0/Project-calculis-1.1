using CommonLibrary;
using Microsoft.Extensions.Logging;
using Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            ByMyCode();
        }

        static void ByMyCode()
        {
            TCPServer server = new TCPServer(9999);
            server.OnNewConnection += async (connection) =>
            {
                AbstractConnection rsaConnection = new RsaDecorator(connection);
                rsaConnection.Connect();
                byte[] message = await rsaConnection.GetMessageAsync();
                Console.WriteLine($"Receive message: {Encoding.UTF8.GetString(message)}");
            };
            server.Listen();
            AbstractConnection connection = new RsaDecorator(new TcpConnection());
            connection.SetEndPoint(IPAddress.Loopback.ToString(), 9999);
            string message = Console.ReadLine();
            connection.Connect();
            connection.Send(Encoding.UTF8.GetBytes(message));
            Console.ReadLine();
        }
    }
}
