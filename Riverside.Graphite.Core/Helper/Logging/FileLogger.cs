using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

			// Delete files older than a day
			var directoryInfo = new DirectoryInfo(logDirectory);
			var oldFiles = directoryInfo.GetFiles()
										.Where(file => file.CreationTime < DateTime.Now.AddDays(-1));
			foreach (var file in oldFiles)
			{
				file.Delete();
			}
		}

		public IDisposable BeginScope<TState>(TState state) => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
			File.AppendAllText(_filePath, logMessage + Environment.NewLine);
		}
	}

	public class TabMangerLogger<T> : ILogger<T>
	{
		private readonly string _filePath;

		public TabMangerLogger()
		{
			string localAppData = UserManager.GraphiteDataPath;
			string logDirectory = Path.Combine(localAppData, AuthService.CurrentUser?.Username, "Logs");
			Directory.CreateDirectory(logDirectory);
			_filePath = Path.Combine(logDirectory, $"TabStates_{DateTime.Now:yyyyMMdd}.txt");

			// Delete files older than a day
			Task.Factory.StartNew(() =>
			{
				var directoryInfo = new DirectoryInfo(logDirectory);
				var oldFiles = directoryInfo.GetFiles()
											.Where(file => file.CreationTime < DateTime.Now.AddDays(-1));
				foreach (var file in oldFiles)
				{
					file.Delete();
				}
			});
		}

		public IDisposable BeginScope<TState>(TState state) => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
			File.WriteAllText(_filePath, logMessage + Environment.NewLine);
		}
	}
}
