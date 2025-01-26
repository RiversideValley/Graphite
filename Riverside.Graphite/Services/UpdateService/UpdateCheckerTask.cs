using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Riverside.Graphite.Services.UpdateService
{
	public sealed class UpdateCheckerTask : IBackgroundTask
	{
		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			var deferral = taskInstance.GetDeferral();

			try
			{
				var services = new ServiceCollection();
				ConfigureServices(services);
				var serviceProvider = services.BuildServiceProvider();

				var logger = serviceProvider.GetRequiredService<ILogger<UpdateCheckerTask>>();
				logger.LogInformation("Update checker task started");

				var updateManager = serviceProvider.GetRequiredService<UpdateManager>();
				var updateResult = await updateManager.CheckAndUpdateAsync();

				logger.LogInformation($"Update check completed. Update installed: {updateResult}");
			}
			catch (Exception ex)
			{
				// Log the exception
			}
			finally
			{
				deferral.Complete();
			}
		}

		private void ConfigureServices(IServiceCollection services)
		{
			services.AddLogging(configure => configure.AddDebug());
			services.AddTransient<GraphiteUpdateClient>();
			services.AddTransient<UpdateManager>();
		}
	}
}
