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
            Task.Run(() =>
            {
                Thread.CurrentThread.Name = "Server thread";
                TcpListener listener = new TcpListener(IPAddress.Any, 13888);
                listener.Start();
                TcpConnection client =new TcpConnection(listener.AcceptTcpClient());
                Console.WriteLine("Server: new connection!");
                client.ReceivingState = ReceivingState.Method;
                client.Connect();
                client.Send(Encoding.UTF8.GetBytes("Also text"));
                //while (client.GetReceivedCount() == 0) Thread.Sleep(100);
                //Console.WriteLine($"Server (available: {client.GetReceivedCount()}): " +
                //    $"has been received message: {Encoding.UTF8.GetString(client.GetReceived().ToArray())}");
            });
            Task.Run(() =>
            {
                Thread.CurrentThread.Name = "Client thread";
                TcpConnection connection =new TcpConnection();
                connection.SetEndPoint("127.0.0.1", 13888);
                connection.ReceivingState = ReceivingState.Method;
                connection.Connect();
                while (connection.GetReceivedCount() == 0) Thread.Sleep(100);
                Console.WriteLine(Encoding.UTF8.GetString(connection.GetReceived().ToArray()));
                //if (connection.IsActive())
                //{
                //    connection.Send(Encoding.UTF8.GetBytes("Text"));
                //}
            });
            Console.ReadLine();
        }
    }
    public class TcpConnection
    {
        private readonly TcpClient client;
        private NetworkStream stream;
        private CancellationTokenSource cancel;

        private string ip;
        private int port;

        private Task receiveTask;

        private readonly ConcurrentQueue<byte> receivedBytes = new ConcurrentQueue<byte>();

        public ReceivingState ReceivingState
        {
            get
            {
                lock (receiveStateObject)
                {
                    return receivingState;
                }
            }
            set
            {
                lock (receiveStateObject)
                {
                    receivingState = value;
                }
            }
        }
        private ReceivingState receivingState = ReceivingState.Both;
        private object receiveStateObject = new object();

        public event ReceiveData OnReceiveData;

        public TcpConnection(TcpClient client)
        {
            this.client = client;
            cancel = new CancellationTokenSource();
            if (IsActive())
            {
                stream = client.GetStream();
                receiveTask = BeginReceive();
            }
        }
        public TcpConnection()
            : this(new TcpClient())
        { }

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

        public void SetEndPoint(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void Connect()
        {
            if (!IsActive())
            {
                client.Connect(ip, port);
                stream = client.GetStream();
                receiveTask = BeginReceive();
            }
        }

        public void Disconnect()
        {
            cancel.Cancel();
            receiveTask.Wait();
            cancel.Dispose();
            client.Close();
        }

        public void Send(byte[] message)
        {
            if (IsActive())
            {
                stream.Write(message);
            }
        }

        private async Task BeginReceive()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesReceived = await stream.ReadAsync(buffer, cancel.Token);

                    lock (receiveStateObject)
                    {
                        if (ReceivingState.HasFlag(ReceivingState.Method))
                            for (int i = 0; i < bytesReceived; i++)
                                receivedBytes.Enqueue(buffer[i]);

                        if (ReceivingState.HasFlag(ReceivingState.Event))
                            OnReceiveData?.Invoke(buffer[0..bytesReceived]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public IEnumerable<byte> GetReceived()
        {
            lock (receivedBytes)
            {
                while (receivedBytes.TryDequeue(out byte b))
                    yield return b;
            }
        }

        public int GetReceivedCount()
        {
            if (receiveTask.Exception != null) throw receiveTask.Exception;
            lock (receivedBytes)
            {
                return receivedBytes.Count;
            }
        }
    }

    [Flags]
    public enum ReceivingState : byte
    {
        Method,
        Event,
        Both
    }
    public delegate void ReceiveData(byte[] message);
}
