using CommunityToolkit.Mvvm.Messaging;
using Riverside.Graphite.Core;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services;
public class SettingsService : ISettingsService
{
	#region MemberProps
	//public SettingsActions Actions { get; set; }
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
				//		Actions = new SettingsActions(AuthService.CurrentUser.Username);
				var settings = await SettingsManager.GetAllSettingsAsync(CurrentUser.Username);
				CoreSettings = new();
				CoreSettings.FromDictionary(settings);

				//CoreSettings = await Actions?.GetSettingsAsync();
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

			//if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
			//{
			//	await Actions?.SettingsContext.Database.MigrateAsync();
			//}
			if (!File.Exists(SettingsManager.GetUserSettingsDbPath(AuthService.CurrentUser.Username)))
			{
				await SettingsManager.InitializeUserSettingsAsync(AuthService.CurrentUser.Username);
			}

			var dictionary = settings.ToDictionary();

			await SettingsManager.UpdateSettingsAsync(user.Username, dictionary);

			//_ = await Actions?.UpdateSettingsAsync(settings);
			// get new from database. 
			//CoreSettings = await Actions?.GetSettingsAsync();
			Initialize();

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