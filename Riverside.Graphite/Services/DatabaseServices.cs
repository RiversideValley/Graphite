using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Actions.Contracts;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using SQLitePCL;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services;
public class DatabaseServices : IDatabaseService
{
	public async Task<Task> InsertUserSettings()
	{
		Batteries_V2.Init();
		if (!AuthService.IsUserAuthenticated)
		{
			return Task.FromResult(false);
		}

		try
		{
			SettingsActions settingsActions = new(AuthService.CurrentUser.Username);
			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
			{
				await settingsActions.SettingsContext.Database.MigrateAsync();
			}
			if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
			{
				if (await settingsActions.GetSettingsAsync() is null)
				{
					_ = await settingsActions.InsertUserSettingsAsync(AppService.AppSettings ?? new Settings(true).Self);
				}
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
			return Task.FromException(ex);
		}

		return Task.CompletedTask;
	}

	public async Task<Task> InsertNewUserSettings()
	{
		Batteries_V2.Init();
		if (!AuthService.IsUserAuthenticated)
		{
			return Task.FromResult(false);
		}

		try
		{
			SettingsActions settingsActions = new(AuthService.NewCreatedUser.Username);
			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser.Username, "Settings", "Settings.db")))
			{
				await settingsActions.SettingsContext.Database.MigrateAsync();
			}
			if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser.Username, "Settings", "Settings.db")))
			{
				if (await settingsActions.GetSettingsAsync() is null)
				{
					_ = await settingsActions.InsertUserSettingsAsync(AppService.AppSettings);
				}
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
			return Task.FromException(ex);
		}

		return Task.CompletedTask;
	}
	public async Task<Task> DatabaseCreationValidation()
	{
		if (!AuthService.IsUserAuthenticated)
		{
			return Task.FromResult(false);
		}

		try
		{
			SettingsActions settingsActions = new(AuthService.CurrentUser.Username);
			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
			{
				await settingsActions.SettingsContext.Database.MigrateAsync();
			}
			if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
			{
				if (settingsActions.SettingsContext.Database.GetPendingMigrations().Any())
				{
					await settingsActions.SettingsContext.Database.MigrateAsync();
				}
					_ = await settingsActions.SettingsContext.Database.CanConnectAsync();
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
		}

		try
		{
			HistoryActions historyActions = new(AuthService.CurrentUser?.Username);
			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "History.db")))
			{
				await historyActions.HistoryContext.Database.MigrateAsync();
			}
			if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "History.db")))
			{
				if (historyActions.HistoryContext.Database.GetPendingMigrations().Any())
				{
					await historyActions.HistoryContext.Database.MigrateAsync();
				}
				_ = await historyActions.HistoryContext.Database.CanConnectAsync();
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating History Database: {ex.Message}");
		}

		try
		{
			DownloadActions downloadActions = new(AuthService.CurrentUser.Username);
			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "Downloads.db")))
			{
				await downloadActions.DownloadContext.Database.MigrateAsync();
			}
			if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "Downloads.db")))
			{
				_ = await downloadActions.DownloadContext.Database.CanConnectAsync();
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating Downloads Database: {ex.Message}");
		}

		return Task.CompletedTask;
	}
}