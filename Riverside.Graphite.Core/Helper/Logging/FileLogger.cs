using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Riverside.Graphite.Core.Helper.Logging
{
    public class FileLogger : ILogger<MigrationManager>
    {
        private readonly string _filePath;

        public FileLogger()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDirectory = Path.Combine(localAppData);
            Directory.CreateDirectory(logDirectory);
            _filePath = Path.Combine(logDirectory, $"MigrationLog_{DateTime.Now:yyyyMMddHHmmss}.txt");
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
            File.AppendAllText(_filePath, logMessage + Environment.NewLine);
        }
    }
}
