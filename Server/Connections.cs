using CommonLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

    public class TcpConnectionToClient:AbstractConnection
    {
        private TcpClient client;
        private BinaryReader reader;
        private BinaryWriter writer;

        private Task receiveTask;

        private List<byte> receivedBytes = new List<byte>();

        public override event ReceiveData OnReceiveData;

        public TcpConnectionToClient(TcpClient client)
        {
            this.client = client;
            reader = new BinaryReader(client.GetStream());
            writer = new BinaryWriter(client.GetStream());
                BeginReceive();
        }

        public override void SetEndPoint(string ip, int port)
        {
            throw new NotImplementedException();
        }

        public override bool IsActive()
        {
            try
            {
                client.GetStream().Write(new byte[0]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Send(byte[] message)
        {
            if (IsActive())
            {
                writer.Write(message);
            }
        }

        private void BeginReceive()
        {
            receiveTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsActive() && client.GetStream().DataAvailable)
                    {
                        byte[] temp = reader.ReadBytes(client.Available);
                        OnReceiveData?.Invoke(temp);
                        receivedBytes.AddRange(temp);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            });
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            Server.connections.Remove(this);
            receiveTask.Dispose();
            client.Close();
            reader.Close();
            writer.Close();
        }

        public override IEnumerable<byte> Receive()
        {
            foreach (var item in receivedBytes)
                yield return item;
            receivedBytes.Clear();
        }
    }
}
