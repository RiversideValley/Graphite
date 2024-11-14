using CommunityToolkit.Mvvm.Messaging;
using Riverside.Graphite.Core;
using Riverside.Graphite.Runtime.Exceptions;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services;
public class SettingsService : ISettingsService
{
	#region MemberProps
	public SettingsActions Actions { get; set; }
	public User CurrentUser { get; set; }
	public Settings CoreSettings { get; set; }
	#endregion
	internal IMessenger Messenger { get; set; }
	public SettingsService()
	{
		Initialize();
		Messenger = App.GetService<IMessenger>();
	}

	public async void Initialize()
	{
		try
		{

			if (AuthService.IsUserAuthenticated)
			{
				CurrentUser = AuthService.CurrentUser ?? null;
				Actions = new SettingsActions(AuthService.CurrentUser.Username);
				CoreSettings = await Actions?.GetSettingsAsync();
			}

		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
		}
	}

	public async Task SaveChangesToSettings(User user, Riverside.Graphite.Core.Settings settings)
	{
		try
		{

			if (!AuthService.IsUserAuthenticated)
			{
				return;
			}

			AppService.AppSettings = settings;
			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
			{
				await Actions?.SettingsContext.Database.MigrateAsync();
			}

			_ = await Actions?.UpdateSettingsAsync(settings);
			// get new from database. 
			CoreSettings = await Actions?.GetSettingsAsync();

			object obj = new();
			lock (obj)
			{
				_ = (Messenger?.Send(new Message_Settings_Actions(EnumMessageStatus.Settings)));
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");

		}
	}
}