using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Methods;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services;
public class DatabaseServices : IDatabaseService
{
	//public async Task<Task> InsertUserSettings()
	//{
	//	Batteries_V2.Init();
	//	if (!AuthService.IsUserAuthenticated)
	//	{
	//		return Task.FromResult(false);
	//	}

	//	try
	//	{
	//		SettingsActions settingsActions = new(AuthService.CurrentUser.Username);
	//		if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
	//		{
	//			await settingsActions.SettingsContext.Database.MigrateAsync();
	//		}
	//		if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
	//		{
	//			if (await settingsActions.GetSettingsAsync() is null)
	//			{
	//				_ = await settingsActions.InsertUserSettingsAsync(AppService.AppSettings ?? new Settings(true).Self);
	//			}
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		ExceptionLogger.LogException(ex);
	//		Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
	//		return Task.FromException(ex);
	//	}

	//	return Task.CompletedTask;
	//}

	//public async Task<Task> InsertNewUserSettings()
	//{
	//	Batteries_V2.Init();
	//	if (!AuthService.IsUserAuthenticated)
	//	{
	//		return Task.FromResult(false);
	//	}

	//	try
	//	{
	//		SettingsActions settingsActions = new(AuthService.NewCreatedUser.Username);
	//		if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser.Username, "Settings", "Settings.db")))
	//		{
	//			await settingsActions.SettingsContext.Database.MigrateAsync();
	//		}
	//		if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser.Username, "Settings", "Settings.db")))
	//		{
	//			if (await settingsActions.GetSettingsAsync() is null)
	//			{
	//				_ = await settingsActions.InsertUserSettingsAsync(AppService.AppSettings);
	//			}
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		ExceptionLogger.LogException(ex);
	//		Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
	//		return Task.FromException(ex);
	//	}

	//	return Task.CompletedTask;
	//}

	private async Task ValidateDatabaseAsync(string username, string dbSubFolder, string dbName, Func<DbContext> contextFactory, string errorMessage)
	{
		try
		{
			var context = contextFactory();
			var dbPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, dbSubFolder, dbName);
			if (!File.Exists(dbPath))
			{
				await context.Database.MigrateAsync();
			}
			if (File.Exists(dbPath))
			{
				if (context.Database.GetPendingMigrations().Any())
				{
					if (!await Methods.ApplyPendingMigrations(context))
						throw new Exception(errorMessage);
				}
				_ = await context.Database.CanConnectAsync();
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating {dbName} Database: {ex.Message}");
		}
	}

	public async Task<Task> DatabaseCreationValidation(User user)
	{
		if (!AuthService.IsUserAuthenticated)
		{
			return Task.FromResult(false);
		}


		//await ValidateDatabaseAsync(AuthService.CurrentUser.Username, "Settings",  "Settings.db", () => new SettingsContext(user.Username), "Can't update your Settings database, please reset your application in the settings page");
		await ValidateDatabaseAsync(AuthService.CurrentUser.Username, "Database", "History.db", () => new HistoryContext(user.Username), "Can't update your History database, please reset your application in the settings page");
		await ValidateDatabaseAsync(AuthService.CurrentUser.Username, "Database", "Downloads.db", () => new DownloadContext(user.Username), "Can't update your Downloads database, please reset your application in the settings page");
		// allow ui to flow. 
		return Task.CompletedTask;
	}

	public async void CreateCollections()
	{

		try
		{
			HistoryActions historyActions = new HistoryActions(AuthService.CurrentUser?.Username);
			string historyPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser?.Username, "Database", "History.db");
			if (!File.Exists(historyPath))
			{
				await historyActions.HistoryContext.Database.MigrateAsync();
			}

			if (File.Exists(historyPath))
			{
				if (await historyActions.HistoryContext.Database.CanConnectAsync())
				{
					historyActions.HistoryContext.CollectionNames.AddRange(new List<CollectionName>
					{
						new CollectionName { Name = "Work", BackgroundBrush = RandomColors.GetRandomSolidColorBrush() },
						new CollectionName { Name = "Personal", BackgroundBrush = RandomColors.GetRandomSolidColorBrush() },
						new CollectionName { Name = "Hobbies", BackgroundBrush = RandomColors.GetRandomSolidColorBrush() },
						new CollectionName { Name = "Following", BackgroundBrush = RandomColors.GetRandomSolidColorBrush() }
					});

					await historyActions.HistoryContext.SaveChangesAsync();
				}
			}
		}
		catch (Exception e)
		{

			ExceptionLogger.LogException(e);
			return;
		}

	}
}

