using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLibrary
{
    /// <summary>
    /// Класс TCP-подключения
    /// </summary>
    public class TcpConnection : AbstractConnection
    {
        private readonly TcpClient client;
        private NetworkStream stream;
        private CancellationTokenSource cancel;

        private string ip;
        private int port;

        /// <summary>
        /// Инициализация подключения на основе подключённого TcpClient
        /// </summary>
        /// <param name="client">Подключённый TcpClient</param>
        public TcpConnection(TcpClient client)
        {
            this.client = client;
            cancel = new CancellationTokenSource();
            if (IsActive)
            {
                stream = client.GetStream();
            }
        }
        /// <summary>
        /// Инициализация подключения по умолчанию
        /// </summary>
        public TcpConnection()
            : this(new TcpClient())
        { }

        /// <inheritdoc/>
        public override bool IsActive
        {
            get
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
        }

        /// <inheritdoc/>
        public override void SetEndPoint(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        /// <inheritdoc/>
        public override void Connect()
        {
            if (!IsActive)
            {
                client.Connect(ip, port);
                stream = client.GetStream();
            }
        }

        public override void Disconnect()
        {
            cancel.Cancel();
            cancel.Dispose();
            cancel = new CancellationTokenSource();
            client.Close();
        }

        public override void Send(byte[] message)
        {
            if (IsActive)
            {
                stream.Write(BitConverter.GetBytes(message.Length));
                stream.Write(message);
            }
        }

        public override async Task<byte[]> GetMessageAsync()
        {
            byte[] lengthBytes = new byte[4];
            await stream.ReadAsync(lengthBytes, cancel.Token);
            int length = BitConverter.ToInt32(lengthBytes);
            byte[] message = new byte[length];
            await stream.ReadAsync(message, cancel.Token);
            return message;
        }
    }

    public class RsaDecorator : AbstractConnection
    {
        private readonly AbstractConnection innerConnection;

        private RSAParameters publicKey;
        private RSAParameters privateKey;

        private readonly KeyGen keyGen = new KeyGen();

        private bool connectionCompleted = false;

        public byte KeysCount => keyGen.KeysCount;

        public RsaDecorator(AbstractConnection connection)
        {
            innerConnection = connection;
        }

        private void SendKey(RSAParameters key)
        {
            var key1 = new RsaSerializable(key);
            var str = JsonSerializer.Serialize(key1);
            innerConnection.Send(Encoding.UTF8.GetBytes(str));
        }
        private async Task<RSAParameters>ReceiveKey()
        {
            Task<byte[]> task = innerConnection.GetMessageAsync();
            await task;
            return JsonSerializer.Deserialize<RsaSerializable>(Encoding.UTF8.GetString(task.Result)).ToRsaParameters();
        }

        public override void Connect()
        {
            if (!innerConnection.IsActive)
                innerConnection.Connect();
            privateKey = keyGen.GetRSAParameters();

            SendKey(privateKey);
            publicKey = ReceiveKey().Result;
            connectionCompleted = true;
        }

        public override void Disconnect()
        {
            connectionCompleted = false;
            innerConnection.Disconnect();
        }

        public override bool IsActive
        {
            get
            {
                return connectionCompleted && innerConnection.IsActive;
            }
        }

        public override async Task<byte[]> GetMessageAsync()
        {
            Task<byte[]> task = innerConnection.GetMessageAsync();
            await task;
            return Decrypt(task.Result);
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
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(publicKey);
            return rsa.Encrypt(message, false);
        }

        private byte[] Decrypt(byte[] message)
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(privateKey);
            return rsa.Decrypt(message, false);
        }

        [Serializable]
        private class RsaSerializable
        {
            public byte[] Exponent { get; set; }
            public byte[] Modulus { get; set; }

            public RsaSerializable(RSAParameters parameters)
            {
                Exponent = parameters.Exponent;
                Modulus = parameters.Modulus;
            }
            public RsaSerializable() { }
            public RSAParameters ToRsaParameters()
            {
                return new RSAParameters() { Exponent = Exponent, Modulus = Modulus };
            }
        }

        private class KeyGen
        {
            internal byte KeysCount { get => keysCount; set => keysCount = value == 0 ? throw new ArgumentOutOfRangeException() : value; }
            private byte keysCount = 2;

            private readonly Queue<RSAParameters> keys = new Queue<RSAParameters>();

            internal KeyGen()
            {
                GenerateKeyToQueue();
            }

            /// <summary>
            ///  Метод, возвращающий ключ
            /// </summary>
            internal RSAParameters GetRSAParameters()
            {
                // Заполнение очереди с ключами до указанного количества +1
                for (int i = 0; i <= KeysCount - keys.Count; i++)
                    GenerateKeyToQueue();

                // Ожидание появления ключей в очереди
                while (keys.Count == 0) Task.Delay(100);

                // Извлечение результата
                lock (keys)
                {
                    return keys.Dequeue();
                }
            }

            /// <summary>
            ///  Метод, генерирующий ключ и добавляющий его в очередь
            /// </summary>
            private Task GenerateKeyToQueue()
            {
                // Запуск асинхронной генерации
                return Task.Run(() =>
                {
                    // Генерация
                    using var rsa = new RSACryptoServiceProvider();
                    RSAParameters result = rsa.ExportParameters(true);

                    // Добавление в очередь
                    lock (keys)
                    {
                        keys.Enqueue(result);
                    }
                });
            }
        }
    }
}
