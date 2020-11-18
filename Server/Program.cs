using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main()
        {
            Server.SetPort(8888);
            new Thread(new ThreadStart(Server.Listen)).Start();
            Server.OnNewConnection += (TcpConnectionToClient client) =>
              {
                  Console.WriteLine("New connection!");
                  client.OnReceiveData += (byte[] message) =>
                  {
                      Console.WriteLine($"Has been received message:{Encoding.UTF8.GetString(message)}");
                      for (int i = 0; i < message.Length; i++)
                          message[i]++;
                      client.Send(message);
                  };
              };
            while (true) ;
        }
    }
}
