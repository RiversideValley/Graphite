using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Riverside.Graphite.Services.UpdateService
{
	public class UpdateManager
	{
		private readonly GraphiteUpdateClient _updateClient;
		private readonly ILogger<UpdateManager> _logger;

		public UpdateManager(GraphiteUpdateClient updateClient, ILogger<UpdateManager> logger)
		{
			_updateClient = updateClient;
			_logger = logger;
		}

		public async Task<bool> CheckAndUpdateAsync()
		{
			try
			{
				_logger.LogInformation("Starting update check");
				var currentVersion = GetCurrentVersion();
				var checkResult = await _updateClient.CheckForUpdatesAsync();

				if (checkResult.StartsWith("Update available"))
				{
					_logger.LogInformation("New version available. Starting download");
					var downloadResult = await _updateClient.DownloadUpdateAsync();

					if (downloadResult == "Update downloaded successfully")
					{
						_logger.LogInformation("Update downloaded. Starting installation");
						var applyResult = await _updateClient.ApplyUpdateAsync();

						if (applyResult == "Update applied successfully")
						{
							_logger.LogInformation("Update installed successfully");
							return true;
						}
					}
				}
				else
				{
					_logger.LogInformation("No updates available");
				}

				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during update process");
				return false;
			}
		}

		private string GetCurrentVersion()
		{
			var package = Package.Current;
			return $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}";
		}
	}
}
