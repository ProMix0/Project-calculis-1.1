using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main()
        {
            Server.SetPort(8888);
            Server.OnNewConnection += (AbstractConnection client) =>
            {
                //client = new RsaDecorator(client,true);
                client.ReceivingState = ReceivingState.Event;
                client.Connect();
                Console.WriteLine("New connection!");
                client.Send(Encoding.UTF8.GetBytes("Still text"));
                client.OnReceiveData += (byte[] message) =>
                {
                    Console.WriteLine($"Has been received message: {Encoding.UTF8.GetString(message)}");
                    client.Send(message);
                };
            };
            Task.Run(Server.Listen).Wait();
        }
    }
}
