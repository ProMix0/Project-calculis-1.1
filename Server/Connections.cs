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
                        OnNewConnection?.Invoke(connection);
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
        public event ReceiveData OnReceiveData;

        private Thread receiveThread;

        public TcpConnectionToClient(TcpClient client)
        {
            this.client = client;
            try
            {
                receiveThread = new Thread(new ThreadStart(() =>
                  {
                      while (true)
                      {
                          OnReceiveData?.Invoke(Receive());
                      }
                  }));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            receiveThread.Start();
        }

        public bool IsActive()
        {
            try
            {
                client.GetStream().Write(new byte[0]);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Send(byte[] message)
        {
            if (IsActive())
            using (BinaryWriter writer = new BinaryWriter(client.GetStream()))
            {
                writer.Write(message);
            }
        }

        public byte[] Receive()
        {
            while (true)
                if (IsActive() && client.GetStream().DataAvailable)
                {
                    using (BinaryReader reader = new BinaryReader(client.GetStream()))
                    {
                        var temp= reader.ReadBytes(client.Available);
                        return temp;
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
        }

        public void Disconnect()
        {
            Server.connections.Remove(this);
            receiveThread.Abort();
        }
    }
}
