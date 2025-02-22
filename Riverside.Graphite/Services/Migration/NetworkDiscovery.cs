using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Migration
{
	public class NetworkDiscovery
	{
		private const int DiscoveryPort = 8080;
		private const string BroadcastMessage = "GRAPHITE_DISCOVERY";
		private const string ResponseMessage = "GRAPHITE_RECEIVER";
		private readonly List<IPEndPoint> discoveredReceivers = new List<IPEndPoint>();

		public event EventHandler<IPEndPoint> ReceiverDiscovered;

		public async Task<List<IPEndPoint>> DiscoverReceiversAsync(CancellationToken cancellationToken)
		{
			discoveredReceivers.Clear();
			using var udpClient = new UdpClient();
			udpClient.EnableBroadcast = true;

			// Start listening for responses
			var listenTask = ListenForResponsesAsync(udpClient, cancellationToken);

			// Broadcast discovery message
			var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
			var message = Encoding.UTF8.GetBytes(BroadcastMessage);

			// Send broadcast multiple times to increase reliability
			for (int i = 0; i < 3 && !cancellationToken.IsCancellationRequested; i++)
			{
				await udpClient.SendAsync(message, message.Length, broadcastEndpoint);
				await Task.Delay(500, cancellationToken);
			}

			// Wait for responses
			await Task.Delay(2000, cancellationToken); // Wait 2 seconds for responses
			return discoveredReceivers;
		}

		private async Task ListenForResponsesAsync(UdpClient udpClient, CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var result = await udpClient.ReceiveAsync(cancellationToken);
					var response = Encoding.UTF8.GetString(result.Buffer);

					if (response == ResponseMessage)
					{
						var receiverEndpoint = new IPEndPoint(result.RemoteEndPoint.Address, DiscoveryPort);
						if (!discoveredReceivers.Contains(receiverEndpoint))
						{
							discoveredReceivers.Add(receiverEndpoint);
							ReceiverDiscovered?.Invoke(this, receiverEndpoint);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Normal cancellation
			}
			catch (Exception ex)
			{
				// Handle or log the exception
				Console.WriteLine($"Error in ListenForResponsesAsync: {ex.Message}");
			}
		}

		public static async Task StartReceiverDiscoveryServiceAsync(CancellationToken cancellationToken)
		{
			using var udpClient = new UdpClient(DiscoveryPort);
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var result = await udpClient.ReceiveAsync(cancellationToken);
					var message = Encoding.UTF8.GetString(result.Buffer);

					if (message == BroadcastMessage)
					{
						var response = Encoding.UTF8.GetBytes(ResponseMessage);
						await udpClient.SendAsync(response, response.Length, result.RemoteEndPoint);
					}
				}
				catch (OperationCanceledException)
				{
					// Normal cancellation
				}
				catch (Exception ex)
				{
					// Handle or log the exception
					Console.WriteLine($"Error in StartReceiverDiscoveryServiceAsync: {ex.Message}");
					await Task.Delay(1000, cancellationToken);
				}
			}
		}
	}
}

