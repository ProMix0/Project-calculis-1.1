using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLibrary
{

    public class TcpConnection : AbstractConnection
    {
        private readonly TcpClient client;
        private NetworkStream stream;
        private CancellationTokenSource cancel;

        private string ip;
        private int port;

        private Task receiveTask;

        private readonly ConcurrentQueue<byte> receivedBytes = new ConcurrentQueue<byte>();

        public override ReceivingState ReceivingState
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

        public override event ReceiveData OnReceiveData;

        public TcpConnection(TcpClient client)
        {
            this.client = client;
            cancel = new CancellationTokenSource();
            if (IsActive())
            {
                stream = client.GetStream();
                receiveTask =  BeginReceive();
            }
        }
        public TcpConnection()
            : this(new TcpClient())
        { }

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
            if (!IsActive())
            {
                client.Connect(ip, port);
                stream = client.GetStream();
                receiveTask = BeginReceive();
            }
        }

        public override void Disconnect()
        {
            cancel.Cancel();
            receiveTask.Wait();
            cancel.Dispose();
            client.Close();
        }

        public override void Send(byte[] message)
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override IEnumerable<byte> GetReceived()
        {
            lock (receivedBytes)
            {
                while (receivedBytes.TryDequeue(out byte b))
                    yield return b;
            }
        }

        public override int GetReceivedCount()
        {
            if (receiveTask.Exception != null) throw receiveTask.Exception;
            lock (receivedBytes)
            {
                return receivedBytes.Count;
            }
        }
    }

    public class RsaDecorator : AbstractConnection
    {
        private AbstractConnection innerConnection;

        private RSAParameters publicKey;
        private RSAParameters privateKey;

        private readonly KeyGen keyGen=new KeyGen();

        public bool ReceiveKeyFirst { get;private set; }

        public override ReceivingState ReceivingState
        {
            get => innerConnection.ReceivingState;
            set => innerConnection.ReceivingState = value;
        }
        public override event ReceiveData OnReceiveData;

        private bool connectionCompleted = false;

        public RsaDecorator(AbstractConnection connection)
        {
            innerConnection = connection;
            innerConnection.OnReceiveData += (byte[] message) =>
              {
                  OnReceiveData?.Invoke(Decrypt(message));
              };
        }
        public RsaDecorator(AbstractConnection connection, bool receiveKeyFirst)
            : this(connection)
        {
            ReceiveKeyFirst = receiveKeyFirst;
        }

        private void SendKey(RSAParameters key)
        {
            RsaSerializable serializableKey = new RsaSerializable(key);
            BinaryFormatter formatter = new BinaryFormatter();
            using MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, serializableKey);
            innerConnection.Send(stream.ToArray());
        }
        private RSAParameters ReceiveKey()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using Stream stream = new MemoryStream();
            while (innerConnection.GetReceivedCount() == 0) Thread.Sleep(100);
            stream.Write(innerConnection.GetReceived().ToArray());
            return ((RsaSerializable)formatter.Deserialize(stream)).GetAsRsaParameters();
        }

        public override void Connect()
        {
            if (!innerConnection.IsActive())
                innerConnection.Connect();
            privateKey = keyGen.GetRSAParameters();
            //try
            //{

            // Try to remove "ReceiveKeyFirst" property
            //if (ReceiveKeyFirst)
            //{
            //    publicKey = ReceiveKey();
            //    SendKey(privateKey);
            //}
            //else
            //{
            SendKey(privateKey);
            publicKey = ReceiveKey();
            //}
            //}
            //catch
            //{
            //    throw new CryptographicException();
            //}
            connectionCompleted = true;
        }

        public override void Disconnect()
        {
            connectionCompleted = false;
            innerConnection.Disconnect();
        }

        public override bool IsActive()
        {
            return connectionCompleted && innerConnection.IsActive();
        }

        public override IEnumerable<byte> GetReceived()
        {
            return Decrypt(innerConnection.GetReceived().ToArray());
        }

        public override void Send(byte[] message)
        {
            innerConnection.Send(Encrypt(message));
        }

        public override void SetEndPoint(string ip, int port)
        {
            innerConnection.SetEndPoint(ip, port);
        }

        private byte[] Encrypt(byte[] message)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(publicKey);
            return rsa.Encrypt(message, false);
        }

        private byte[] Decrypt(byte[] message)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(privateKey);
            return rsa.Decrypt(message, false);
        }

        public override int GetReceivedCount()
        {
            return innerConnection.GetReceivedCount();
        }

        [Serializable]
        public class RsaSerializable
        {
            public byte[] Exponent;
            public byte[] Modulus;

            public RsaSerializable(RSAParameters parameters)
            {
                Exponent = parameters.Exponent;
                Modulus = parameters.Modulus;
            }
            public RSAParameters GetAsRsaParameters()
            {
                return new RSAParameters() { Exponent = Exponent, Modulus = Modulus };
            }
        }

        private class KeyGen
        {

            internal int KeysCount { get; set; } = 2;

            private Queue<RSAParameters> Keys;

            /// <summary>
            ///  Метод, возвращающий ключ
            /// </summary>
            internal RSAParameters GetRSAParameters()
            {
                // Заполнение очереди с ключами до указанного количества +1
                for (int i = 0; i <= KeysCount - Keys.Count; i++)
                    GenerateKeyToQueue();

                // Ожидание появления ключей в очереди
                while (Keys.Count == 0)Thread.Sleep(100);

                // Извлечение результата
                lock (Keys)
                {
                    return Keys.Dequeue();
                }
            }

            internal KeyGen()
            {

                Keys = new Queue<RSAParameters>();
                GenerateKeyToQueue();

            }

            /// <summary>
            ///  Метод, генерирующий ключ и добавляющий его в очередь
            /// </summary>
            private void GenerateKeyToQueue()
            {
                // Запуск асинхронной генерации
                Task.Run(() =>
                {
                    // Генерация
                    using var rsa = new RSACryptoServiceProvider();
                    RSAParameters result = rsa.ExportParameters(true);

                    // Добавление в очередь
                    lock (Keys)
                    {
                        Keys.Enqueue(result);
                    }
                });
            }
        }
    }

    [Flags]
    public enum ReceivingState : byte
    {
        Method,
        Event,
        Both=Event|Method
    }
}
