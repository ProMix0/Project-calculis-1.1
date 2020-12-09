using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public abstract class AbstractConnection
    {
        public abstract bool IsActive();
        public abstract void SetEndPoint(string ip, int port);
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(byte[] message);
        public abstract Task<byte[]> GetMessageAsync();
    }
}
