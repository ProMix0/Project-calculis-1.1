using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLibrary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Server
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private AbstractServer abstractServer;
        private readonly List<Interviewer> interviewers = new();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            abstractServer = new TCPServer(3490);
            abstractServer.OnNewConnection += (client) => 
            {
                interviewers.Add(new(client));
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    public class Interviewer
    {
        private AbstractConnection client;

        public Interviewer(AbstractConnection client)
        {
            this.client = client;
        }

        public async Task BeginDialog()
        {
            //TODO
        }
    }
}
