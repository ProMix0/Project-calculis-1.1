using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Client_library
{
    public class ConnectionToServer
    {
        private TcpClient client = new TcpClient();

        public bool IsActive()
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

        public void Connect(string ip, int port)
        {
            client.Connect(ip, port);
        }

        public void Send(byte[] message)
        {
            if (IsActive())
            using (BinaryWriter writer = new BinaryWriter(client.GetStream()))
            {
                writer.Write(message);
            }
        }

        public byte[] Receive() // BUG Не происходит получения сообщения, так как IsActive() возвращает false
        {
            while (true)
                if (IsActive() && client.GetStream().DataAvailable)
                {
                    using (BinaryReader reader = new BinaryReader(client.GetStream()))
                    {
                        return reader.ReadBytes(client.Available);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
        }
    }
}
