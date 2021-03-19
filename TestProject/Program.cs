using Microsoft.Extensions.Logging;
using Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            JsonLogger logger = new JsonLogger(100, "Testing in test project");
            logger.Log(LogLevel.Information, "Something");
            using IDisposable disposable = logger.BeginScope("Test scopes");
            logger.Log(LogLevel.Warning, "In scopes");
        }
    }
}
