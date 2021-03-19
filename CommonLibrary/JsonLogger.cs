using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace Server
{
    public class JsonLogger
    {
        private List<Scope> scopes=new List<Scope>();
        private string identifier;
        private JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };

        public LogLevel LevelToLog { get; set; } = LogLevel.Information;

        private LogRecord[] logRecords;
        private int pointer = -1;
        private int lastWrite = 0;

        public JsonLogger(int recordsCount, string identifier)
        {
            logRecords = new LogRecord[recordsCount];
            this.identifier = identifier;
        }

        public IDisposable BeginScope(string message)
        {
            return new Scope(message, scopes);
        }

        public void Log(LogLevel logLevel, string message)
        {
            if (logLevel >= LevelToLog)
            {
                LogRecord record = new LogRecord(logLevel, message, scopes.ToArray());
                if (++pointer >= logRecords.Length) pointer = 0;

                logRecords[pointer] = record;

                if (++lastWrite >= logRecords.Length || logLevel >= LogLevel.Warning)
                {
                    Write().Wait();
                }
            }
        }

        private async Task Write()
        {
            using Stream stream = File.OpenWrite($"{identifier}.debug");
            lastWrite = 0;
            for (int i = ++pointer; i < pointer + logRecords.Length; i++)
            {
                if (logRecords[i % logRecords.Length] != null)
                {
                   await JsonSerializer.SerializeAsync(stream, logRecords[i % logRecords.Length], options);
                }
            }
            logRecords = new LogRecord[logRecords.Length];
            pointer = 0;
        }
    }

    public class Scope : IDisposable
    {
        private bool disposed = false;

        public string Message { get; set; }
        private List<Scope> scopes;

        public Scope(string message, List<Scope> scopes)
        {
            Message = message;
            this.scopes = scopes;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                scopes.Remove(this);
            }

            disposed = true;
        }
    }

    public class LogRecord
    {
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public Scope[] Scopes { get; set; }

        public LogRecord(LogLevel logLevel, string message, Scope[] scopes)
        {
            Message = message;
            Scopes = scopes;
            LogLevel = logLevel.ToString();
        }
    }
}
