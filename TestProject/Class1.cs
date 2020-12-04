using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject1
{
    class Program1
    {
        public static async Task Main(string[] args)
        {
            Task t1 = Task.Run(() =>
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 13888);
                listener.Start();
                TcpConnection1 client = new TcpConnection1(listener.AcceptTcpClient());
                client.ReceivingState = ReceivingState.Method;
                Console.WriteLine("Server: new connection!");

                while (client.GetReceivedCount() == 0) Thread.Sleep(100);
                Console.WriteLine($"Server (available: {client.GetReceivedCount()}): " +
                    $"has been received message: {Encoding.UTF8.GetString(client.GetReceived().ToArray())}");
            });
            Task t2 = Task.Run(() =>
            {
                TcpConnection1 connection = new TcpConnection1();
                connection.SetEndPoint("127.0.0.1", 13888);
                connection.ReceivingState = ReceivingState.Method;
                connection.Connect();
                if (connection.IsActive)
                {
                    connection.Send(Encoding.UTF8.GetBytes("Text"));
                }
                connection.Disconnect();
            });
            await Task.WhenAll(t1, t2);
            Console.ReadKey();
        }
    }

    public class TcpConnection1
    {
        private readonly TcpClient client;
        private NetworkStream stream;

        private string ip;
        private int port;

        private Task receiveTask;

        private readonly ConcurrentQueue<byte> receivedBytes = new ConcurrentQueue<byte>();
        private CancellationTokenSource cts;

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
        private readonly object receiveStateObject = new object();

        public event ReceiveData OnReceiveData;

        public TcpConnection1(TcpClient client)
        {
            this.client = client;
            if (client.Connected)
            {
                cts = new CancellationTokenSource();
                receiveTask = BeginReceive();
            }
        }
        public TcpConnection1()
            : this(new TcpClient())
        { }

        public bool IsActive => client.Connected;

        public void SetEndPoint(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void Connect()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            client.Connect(ip, port);
            stream = client.GetStream();
            receiveTask = BeginReceive();
        }

        public void Disconnect()
        {
            cts?.Cancel();
            receiveTask.Wait();
            cts?.Dispose();
            cts = null;
            client.Close();
        }

        public void Send(byte[] message)
        {
            if (IsActive)
            {
                stream.Write(message);
            }
        }

        private async Task BeginReceive()
        {
            byte[] buffer = new byte[65536];
            try
            {
                while (true)
                {
                    int bytesReceived = await stream.ReadAsync(buffer, cts.Token);

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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public IEnumerable<byte> GetReceived()
        {
            lock (receiveStateObject)
            {
                while (receivedBytes.TryDequeue(out byte b))
                    yield return b;
            }
        }
        public int GetReceivedCount()
        {
            lock (receiveStateObject)
            {
                return receivedBytes.Count;
            }
        }
    }

    [Flags]
    public enum ReceivingState : byte
    {
        Method = 1,
        Event = 2,
        Both = Method | Event
    }

    public delegate void ReceiveData(byte[] message);
}
