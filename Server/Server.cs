using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
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

    public class TCPServer : AbstractServer
    {
        private readonly TcpListener listener;

        public TCPServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public override void Listen()
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
        }
    }
}
