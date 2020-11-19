using CommonLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client_library
{
    public class TcpConnectionToServer:AbstractConnection
    {
        private TcpClient client = new TcpClient();
        private BinaryReader reader;
        private BinaryWriter writer;

        private string ip;
        private int port;

        private Task receiveTask;

        private List<byte> receivedBytes=new List<byte>();

        public override event ReceiveData OnReceiveData;

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

        public override void SetEndPoint(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public override void Connect()
        {
            client.Connect(ip, port);
            reader = new BinaryReader(client.GetStream());
            writer = new BinaryWriter(client.GetStream());
            BeginReceive();
        }

        public override void Disconnect()
        {
            client.Close();
            reader.Close();
            writer.Close();
        }

        public override void Send(byte[] message)
        {
            if (IsActive())
                writer.Write(message);
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

        public override IEnumerable<byte> Receive()
        {
            foreach (var item in receivedBytes)
                yield return item;
            receivedBytes.Clear();
        }
    }
}
