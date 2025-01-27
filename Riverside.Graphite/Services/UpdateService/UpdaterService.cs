using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Riverside.Graphite.Services.UpdateService
{
	public class UpdaterService
	{
		private readonly GraphiteUpdateClient _updateClient;
		private readonly ILogger<UpdaterService> _logger;

		public UpdaterService(GraphiteUpdateClient updateClient, ILogger<UpdaterService> logger)
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

				_logger.LogInformation($"Current version: {currentVersion}, Check result: {checkResult}");

				if (checkResult.StartsWith("Update available"))
				{
					_logger.LogInformation("New version available. Starting download and installation");
					return await DownloadAndInstallUpdateAsync();
				}
				else
				{
					_logger.LogInformation("No updates available");
					ShowNotification("No updates available", "You're running the latest version.");
					return false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during update check and installation");
				ShowNotification("Update Error", $"An error occurred while checking for updates: {ex.Message}");
				return false;
			}
		}

		private string GetCurrentVersion()
		{
			var package = Package.Current;
			return $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}";
		}

		private async Task<bool> DownloadAndInstallUpdateAsync()
		{
			try
			{
				var downloadResult = await _updateClient.DownloadUpdateAsync();
				_logger.LogInformation($"Download result: {downloadResult}");

				if (downloadResult == "Update downloaded successfully")
				{
					var installResult = await _updateClient.ApplyUpdateAsync();
					_logger.LogInformation($"Install result: {installResult}");

					if (installResult == "Update applied successfully")
					{
						_logger.LogInformation("Update installed successfully");
						ShowNotification("Update Successful", "FireBrowserWinUi has been updated. Please restart the application.");
						return true;
					}
				}

				ShowNotification("Update Failed", "Failed to install update. Please try again later.");
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to install update");
				ShowNotification("Update Failed", $"Failed to install update: {ex.Message}");
				return false;
			}
		}

		private void ShowNotification(string title, string content)
		{
			_logger.LogInformation($"Showing notification: {title} - {content}");
			var notificationManager = AppNotificationManager.Default;
			var notification = new AppNotification(content);
			notification.Expiration = DateTimeOffset.Now.AddDays(1);
			notificationManager.Show(notification);
		}
	}
}
