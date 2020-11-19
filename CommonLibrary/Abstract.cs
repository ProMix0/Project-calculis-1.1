using System;
using System.Collections;
using System.Collections.Generic;

namespace CommonLibrary
{
    public abstract class AbstractConnection
    {
        public delegate void ReceiveData(byte[] message);
        public abstract event ReceiveData OnReceiveData;
        public abstract bool IsActive();
        public abstract void SetEndPoint(string ip, int port);
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(byte[] message);
        public abstract IEnumerable<byte> Receive();
    }
}
