
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;

namespace Riverside.Graphite.Services.Signalr
{
	public class HubService
	{
		public HubConnection _hubConnection { get; set; }

		public HubService()
		{
			Initialize();
		}
		private async void Initialize()
		{
			try
			{
				_hubConnection = new HubConnectionBuilder()
					.WithUrl(@"https://energy.service.signalr.net/azurechat")
					.WithAutomaticReconnect()
					.ConfigureLogging(logging => logging.AddConsole())
					.Build();

				_hubConnection.Closed += async (error) => await _hubConnection.StartAsync();
				_hubConnection.On<string>("sendNotify", async (message) => ExceptionLogger.LogInformation($"{message}\r\n"));
				await _hubConnection.StartAsync();
				Console.WriteLine("Listening for a message");
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
		}
		//(https://github.com/microsoft/WindowsAppSDK/discussions/3561)
	}
}
