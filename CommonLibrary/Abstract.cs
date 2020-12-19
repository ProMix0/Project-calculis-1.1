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
        /// Устанавливающий конечную точку подклбчения
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
        /// Методасинхронного получения данных
        /// </summary>
        /// <returns>Задача, получающая результат</returns>
        public abstract Task<byte[]> GetMessageAsync();
    }
}
