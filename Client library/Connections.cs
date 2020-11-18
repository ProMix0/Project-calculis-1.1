using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Client_library
{
    public class ConnectionToServer
    {
        private TcpClient client = new TcpClient();

        public bool Disposed { get => !client.Connected; }

        public void Connect(string ip, int port)
        {
            client.Connect(ip, port);
        }

        public void Send(byte[] message)
        {
            using (BinaryWriter writer = new BinaryWriter(client.GetStream()))
            {
                writer.Write(message);
            }
        }

        public byte[] Receive()
        {
            while (client.Available == 0) Thread.Sleep(100);
            using (BinaryReader reader = new BinaryReader(client.GetStream()))
            {
                return reader.ReadBytes(client.Available);
            }
        }
    }
}
