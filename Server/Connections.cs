using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public static class Server
    {
        private static TcpListener listener;

        public delegate void NewConnectionHandler(TcpConnectionToClient client);
        public static event NewConnectionHandler OnNewConnection;

        internal static List<TcpConnectionToClient> connections = new List<TcpConnectionToClient>();

        public static void SetPort(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public static IEnumerable<TcpConnectionToClient> GetConnections()
        {
            foreach (var item in connections)
                yield return item;
        }

        public static void Listen()
        {
            while (true)
            {
                if (listener == null) throw new ArgumentNullException();
                try
                {
                    listener.Start();
                    while (true)
                    {
                        TcpConnectionToClient connection = new TcpConnectionToClient(listener.AcceptTcpClient());
                        OnNewConnection(connection);
                        connections.Add(connection);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    listener?.Stop();
                }
            }
        }
    }

    public class TcpConnectionToClient
    {
        private TcpClient client;

        public delegate void ReceiveData(byte[] message);
        public event ReceiveData OnReceiveData=(byte[] message)=> { };

        private Thread receiveThread;

        public TcpConnectionToClient(TcpClient client)
        {
            this.client = client;
            receiveThread = new Thread(new ThreadStart(() =>
              {
                  while (true)
                  {
                      OnReceiveData(Receive());
                  }
              }));
            receiveThread.Start();
        }

        public void Send(byte[] message)
        {
            using(BinaryWriter writer = new BinaryWriter(client.GetStream()))
            {
                writer.Write(message);
            }
        }

        public byte[] Receive()
        {
            while (client.Available == 0) Thread.Sleep(100);
            using (BinaryReader reader= new BinaryReader(client.GetStream()))
            {
                return reader.ReadBytes(client.Available);
            }
        }

        public void Disconnect()
        {
            Server.connections.Remove(this);
            receiveThread.Abort();
        }
    }
}
