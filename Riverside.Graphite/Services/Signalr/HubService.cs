
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Signalr
{
	public class HubService
	{
		public HubService() {

			Initialize(); 
		}
		private HubConnection _hubConnection;
		private async void Initialize()
		{
			try
			{
				_hubConnection = new HubConnectionBuilder()
					//.WithUrl(@"https://energy.service.signalr.net/azurechat")
					.WithUrl("http://localhost:5000/message")
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
