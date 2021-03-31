using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CommonLibrary
{
    /// <summary>
    /// Абстрактный класс подключения
    /// </summary>
    public abstract class AbstractConnection
    {
        /// <summary>
        /// Текущее состояние подключения
        /// </summary>
        public abstract bool IsActive { get; }

        /// <summary>
        /// Устанавливающий конечную точку подключения
        /// </summary>
        /// <param name="ip">IP-адресс</param>
        /// <param name="port">Порт конечной точки</param>
        public abstract void SetEndPoint(string ip, int port);

        /// <summary>
        /// Осуществляет подключение
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Отключает соединение
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Отправляет сообщение через подключение
        /// </summary>
        /// <param name="message">Сообщение в виде массива байт</param>
        public abstract void Send(byte[] message);

        /// <summary>
        /// Метод асинхронного получения данных
        /// </summary>
        /// <returns>Задача, получающая результат</returns>
        public abstract Task<byte[]> GetMessageAsync();
    }

    public abstract class AbstractServer
    {
        public delegate void NewConnectionHandler(AbstractConnection client);
        public event NewConnectionHandler OnNewConnection;

        public abstract void Listen();

        protected void RaiseNewConnectionHandler(AbstractConnection client)
        {
            OnNewConnection?.Invoke(client);
        }
    }
}
