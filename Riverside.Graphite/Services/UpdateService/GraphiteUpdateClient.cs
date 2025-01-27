using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.UpdateService
{
	public class GraphiteUpdateClient
	{
		private const int TcpPort = 8888;
		private const string ServerName = "localhost";
		private readonly ILogger<GraphiteUpdateClient> _logger;

		public GraphiteUpdateClient(ILogger<GraphiteUpdateClient> logger)
		{
			_logger = logger;
		}

		public async Task<string> CheckForUpdatesAsync()
		{
			return await SendCommandAsync("checkupdates");
		}

		public async Task<string> DownloadUpdateAsync()
		{
			return await SendCommandAsync("downloadupdate");
		}

		public async Task<string> ApplyUpdateAsync()
		{
			return await SendCommandAsync("applyupdate");
		}

		private async Task<string> SendCommandAsync(string command)
		{
			try
			{
				using var tcpClient = new TcpClient();
				await tcpClient.ConnectAsync(ServerName, TcpPort);

				using var stream = tcpClient.GetStream();
				using var reader = new StreamReader(stream);
				using var writer = new StreamWriter(stream) { AutoFlush = true };

				await writer.WriteLineAsync(command);
				var response = await reader.ReadLineAsync();

				return response ?? "No response received";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in TCP communication: {command}");
				return $"Error: {ex.Message}";
			}
		}
	}
}
