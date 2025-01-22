using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Actions.Contracts;
using Riverside.Graphite.Data.Core.Methods;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Data.Common;
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

    private async Task ValidateDatabaseAsync(string username, string dbSubFolder, string dbName,  Func<DbContext> contextFactory, string errorMessage)
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

    public async Task<Task> DatabaseCreationValidation()
    {
        if (!AuthService.IsUserAuthenticated)
        {
            return Task.FromResult(false);
        }

        await ValidateDatabaseAsync(AuthService.CurrentUser.Username, "Settings",  "Settings.db", () => new SettingsContext(AuthService.CurrentUser.Username), "Can't update your Settings database, please reset your application in the settings page");
        await ValidateDatabaseAsync(AuthService.CurrentUser.Username, "Database",  "History.db", () => new HistoryContext(AuthService.CurrentUser.Username), "Can't update your History database, please reset your application in the settings page");
        await ValidateDatabaseAsync(AuthService.CurrentUser.Username, "Database",  "Downloads.db", () => new DownloadContext(AuthService.CurrentUser.Username), "Can't update your Downloads database, please reset your application in the settings page");
		// allow ui to flow. 
		return Task.CompletedTask;
    }
}

	