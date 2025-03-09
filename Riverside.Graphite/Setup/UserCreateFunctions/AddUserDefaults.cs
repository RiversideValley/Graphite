using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Setup.UserCreateFunctions
{
	public class AddUserDefaults
	{

		public static async void CreateCollections(User User)
		{

			try
			{
				HistoryActions historyActions = new HistoryActions(User.Username);
				string historyPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, User.Username, "Database", "History.db");
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
			catch (Exception)
			{

				throw;
			}
		}
		public static async void CreateNewSettings(User user)
		{
			try
			{

				await SettingsManager.InitializeUserSettingsAsync(user.Username);
				await SettingsManager.UpdateSettingsAsync(user.Username ,AppService.AppSettings.ToDictionary()); 
				
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
			}

		}

		public static async Task CopyImageToUserDirectory(Riverside.Graphite.Core.User user, string selectedImageName)
		{
			try
			{
				StorageFolder destinationFolder = await StorageFolder.GetFolderFromPathAsync(
					Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, user.Username));
				StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Riverside.Graphite.Assets/Assets/{selectedImageName}"));
				_ = await imageFile.CopyAsync(destinationFolder, "profile_image.jpg", NameCollisionOption.ReplaceExisting);
			}
			catch (Exception ex)
			{
				// Consider using a logging framework instead of Console.WriteLine
				System.Diagnostics.Debug.WriteLine($"Error copying image: {ex.Message}");
			}
		}
	}
}
