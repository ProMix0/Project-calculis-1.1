using System;
using System.Collections.Generic;
using System.Net;
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
        /// Создание объекта подключения на основе подключённого TcpClient
        /// </summary>
        /// <param name="client">Подключённый TcpClient</param>
        public TcpConnection(TcpClient client)
        {
            this.client = client;
            cancel = new CancellationTokenSource();
        }
        /// <summary>
        /// Инициализация TCP-подключения
        /// </summary>
        public TcpConnection()
            : this(new TcpClient())
        { }

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

        public override void SetEndPoint(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public override void Connect()
        {
            if (!IsActive)
                client.Connect(ip, port);
            stream = client.GetStream();
        }

        public override void Disconnect()
        {
            //Остановка потока прослушивания
            cancel.Cancel();
            cancel.Dispose();
            cancel = new CancellationTokenSource();

            //Закрытие внутреннего подключения
            client.Close();
        }

        public override void Send(byte[] message)
        {
            //Отправка длины сообщения
            stream.Write(BitConverter.GetBytes(message.Length));

            //Отправка сообщения
            stream.Write(message);
        }

        public override async Task<byte[]> GetMessageAsync()
        {
            //Получение длины сообщения
            byte[] lengthBytes = new byte[4];
            await stream.ReadAsync(lengthBytes, cancel.Token);
            int length = BitConverter.ToInt32(lengthBytes);

            //Получение сообщения
            byte[] message = new byte[length];
            await stream.ReadAsync(message, cancel.Token);

            return message;
        }
    }

    /// <summary>
    /// RSA-декоратор подкключения
    /// </summary>
    public class RsaDecorator : AbstractConnection
    {
        private readonly AbstractConnection innerConnection;

        private RSAParameters publicKey;
        private RSAParameters privateKey;

        private readonly KeyGen keyGen = new KeyGen();

        private bool connectionCompleted = false;

        /// <summary>
        /// Задаёт и возвращает количество хранящихся ключей
        /// </summary>
        public byte KeysCount => keyGen.KeysCount;

        /// <summary>
        /// Создаёт новый экземпляр
        /// </summary>
        /// <param name="connection">Внутренне подключение, через которое будут передаваться данные</param>
        public RsaDecorator(AbstractConnection connection)
        {
            innerConnection = connection;
        }

        /// <summary>
        /// Передаёт свой публичный ключ
        /// </summary>
        /// <param name="key">Публичный ключ</param>
        private void SendKey(RSAParameters key)
        {
            //Сериализация ключа
            var key1 = new RsaSerializable(key);
            var str = JsonSerializer.Serialize(key1);

            //Отправка ключа
            innerConnection.Send(Encoding.UTF8.GetBytes(str));
        }
        /// <summary>
        /// Принимает удалённый публичный ключ
        /// </summary>
        /// <returns>Задача получения ключа</returns>
        private async Task<RSAParameters>ReceiveKey()
        {
            //Получение ключа от внутреннего подключения
            Task<byte[]> task = innerConnection.GetMessageAsync();
            await task;

            //Десериализация ключа
            return JsonSerializer.Deserialize<RsaSerializable>(Encoding.UTF8.GetString(task.Result)).ToRsaParameters();
        }

        /// <summary>
        /// Подключается к конечной точке
        /// </summary>
        public override void Connect()
        {
            if (!innerConnection.IsActive)
                innerConnection.Connect();

            //Получение пары ключей
            privateKey = keyGen.GetRSAParameters();

            //Обмен ключами
            SendKey(privateKey);
            publicKey = ReceiveKey().Result;

            connectionCompleted = true;
        }

        /// <summary>
        /// Разрывает установленное подключение
        /// </summary>
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
            //Получает сообщение от внутреннего подключения
            Task<byte[]> task = innerConnection.GetMessageAsync();
            await task;

            //Расшифровка сообщения
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

        /// <summary>
        /// Расшифровывает данные на основе закрытого ключа
        /// </summary>
        /// <param name="message">Зашифрованное сообщение</param>
        /// <returns>Расшифрованное сообщение</returns>
        private byte[] Encrypt(byte[] message)
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(publicKey);
            return rsa.Encrypt(message, false);
        }

        /// <summary>
        /// Зашифровывает данные на основе удалённого публичного ключа
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <returns>Зашифрованное сообщение</returns>
        private byte[] Decrypt(byte[] message)
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(privateKey);
            return rsa.Decrypt(message, false);
        }

        /// <summary>
        /// Класс для передачи публичного ключа
        /// </summary>
        [Serializable]
        private class RsaSerializable
        {
            public byte[] Exponent { get; set; }
            public byte[] Modulus { get; set; }

            /// <summary>
            /// Сохраняет открытый ключ из <с>RSAParameters</с>
            /// </summary>
            /// <param name="parameters"></param>
            public RsaSerializable(RSAParameters parameters)
            {
                Exponent = parameters.Exponent;
                Modulus = parameters.Modulus;
            }
            public RsaSerializable() { }
            /// <summary>
            /// Создаёт <c>RSAParameters</c> из имеющихся данных
            /// </summary>
            /// <returns>Восстановленный экземпляр <c>RSAParameters</c></returns>
            public RSAParameters ToRsaParameters()
            {
                return new RSAParameters() { Exponent = Exponent, Modulus = Modulus };
            }
        }

        /// <summary>
        /// Класс для генерации ключей
        /// </summary>
        private class KeyGen
        {
            internal byte KeysCount { get => keysCount; set => keysCount = value <= 0 ? throw new ArgumentOutOfRangeException() : value; }
            private byte keysCount = 2;

            /// <summary>
            /// Очередь, хранящая сгенерированные ключи
            /// </summary>
            private readonly Queue<RSAParameters> keys = new Queue<RSAParameters>();

            internal KeyGen(){}

            /// <summary>
            ///  Возвращающает сгенерированный ключ
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
            ///  Генерируюет ключ и добавляет его в очередь
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

    public class TCPServer : AbstractServer
    {
        private readonly TcpListener listener;

        public TCPServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public override Task Listen()
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        listener.Start();
                        while (true)
                        {
                            AbstractConnection connection = new TcpConnection(listener.AcceptTcpClient());
                            RaiseNewConnectionHandler(connection);
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
            });
        }
    }
}
