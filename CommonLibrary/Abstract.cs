using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CommonLibrary
{
    public abstract class AbstractConnection
    {

        public delegate void ReceiveData(byte[] message);
        public abstract event ReceiveData OnReceiveData;

        public abstract ReceivingState ReceivingState { get; set; }

        public abstract bool IsActive();
        public abstract void SetEndPoint(string ip, int port);
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(byte[] message);
        public abstract IEnumerable<byte> GetReceived();
        public abstract int GetReceivedCount();
    }
}
