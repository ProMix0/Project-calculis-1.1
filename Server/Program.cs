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
            Server.OnNewConnection += async (AbstractConnection client) =>
            {
                client = new RsaDecorator(client);
                client.Connect();
                Console.WriteLine("New connection!");
                //client.Send(Encoding.UTF8.GetBytes("Still text"));
                try
                {
                    while (true)
                    {
                        Task<byte[]> task = client.GetMessageAsync();
                        await task;
                        Console.WriteLine($"Has been received message: {Encoding.UTF8.GetString(task.Result)}");
                        client.Send(task.Result);
                    }
                }
                catch
                {
                    Console.WriteLine($"Connection has been closed");
                }
            };
            Task.Run(Server.Listen);
            Console.ReadLine();
        }
    }
}
