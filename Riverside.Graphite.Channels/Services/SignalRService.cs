// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR.Management;

namespace FireCore.Services
{
	public interface IHubContextStore
	{
		ServiceHubContext MessageHubContext { get; set; }
		ServiceHubContext ChatHubContext { get; set; }
	}
	public class SignalRService : IHostedService, IHubContextStore, IDisposable
	{
		private const string ChatHub = "Chat";
		private const string MessageHub = "AzureChat";
		private readonly IConfiguration _configuration;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger<SignalRService> _logger;
		public required ServiceHubContext MessageHubContext { get; set; }
		public required ServiceHubContext ChatHubContext { get; set; }

		public SignalRService(IConfiguration configuration, ILoggerFactory loggerFactory, ILogger<SignalRService> logger)
		{
			_configuration = configuration;
			_loggerFactory = loggerFactory;
			_logger = logger;
		}

		async Task IHostedService.StartAsync(CancellationToken cancellationToken)
		{
			using var serviceManager = new ServiceManagerBuilder()
				.WithOptions(o => o.ConnectionString = _configuration["Azure:SignalR:ConnectionString"])
				.WithLoggerFactory(_loggerFactory)
				.WithNewtonsoftJson()
				.BuildServiceManager();

			MessageHubContext = await serviceManager.CreateHubContextAsync(MessageHub, cancellationToken);
			ChatHubContext = await serviceManager.CreateHubContextAsync(ChatHub, cancellationToken);
		}


		Task IHostedService.StopAsync(CancellationToken cancellationToken)
		{
			return Task.WhenAll(Dispose(MessageHubContext!), Dispose(ChatHubContext!));
		}

		private static Task Dispose(ServiceHubContext hubContext)
		{
			if (hubContext == null)
			{
				return Task.CompletedTask;
			}
			return hubContext.DisposeAsync();
		}

		public void Dispose()
		{
			MessageHubContext?.Dispose();
			ChatHubContext?.Dispose();
		}
	}
}