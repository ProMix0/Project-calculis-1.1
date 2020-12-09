using CommonLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public static class Server
    {
        private static TcpListener listener;

        public delegate void NewConnectionHandler(AbstractConnection client);
        public static event NewConnectionHandler OnNewConnection;

        public static void SetPort(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public static void Listen()
        {
            while (true)
            {
                if (listener == null) throw new ArgumentNullException();
                //try
                //{
                    listener.Start();
                    while (true)
                    {
                        AbstractConnection connection = new TcpConnection(listener.AcceptTcpClient());
                        OnNewConnection?.Invoke(connection);
                    }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}
                //finally
                //{
                //    listener?.Stop();
                //}
            }
        }
    }
}
