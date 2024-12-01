using FireCore.Services;
using Microsoft.AspNetCore.SignalR;

namespace Riverside.Graphite.Channels
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		IServiceScopeFactory _serviceScope;
		public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory)
		{
			_logger = logger;
			_serviceScope = serviceScopeFactory;	
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			using var scope = _serviceScope.CreateScope();
			var sigMsgContext = scope.ServiceProvider.GetService<SignalRService>();
			while (!stoppingToken.IsCancellationRequested)
			{
				if (_logger.IsEnabled(LogLevel.Information))
				{
					await sigMsgContext!.MessageHubContext.Clients.All.SendAsync("sendNotify", "Message from worker " + DateTime.Now.ToString(), stoppingToken);
					
					_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				}
				await Task.Delay(10000, stoppingToken);
			}
		}
	}
}
