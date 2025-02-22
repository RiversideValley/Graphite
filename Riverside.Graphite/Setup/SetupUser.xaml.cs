using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Assets;
using Riverside.Graphite.Core;
using Riverside.Graphite.Core.Helper;
using Riverside.Graphite.Core.Models;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite
{
	public sealed partial class SetupUser : Page
	{
		private string selectedImageName = "clippy.png";
		readonly DatabaseServices databaseServices = new();	
		public SetupUser()
		{
			databaseServices = new();
			InitializeComponent();

		}

		private void ProfileImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ProfileImage.SelectedItem is string selectedItem)
			{
				selectedImageName = $"{selectedItem}.png";
				Pimg.ProfilePicture = new ImageLoader().LoadImage(selectedImageName);
			}
		}

		private void UserName_TextChanged(object sender, TextChangedEventArgs e)
		{
			UsrBox.Text = UserName.Text;
		}

		private async void Create_Click(object sender, RoutedEventArgs e)
		{
			await UserManager.InitializeAsync(); // This is a static method, so it should be called on the class, not an instance
			await CreateUserAndNavigate();
		}

		private async Task CreateUserAndNavigate()
		{
			await CreateUserOnStartup();
			await InPrivateUser();
			_ = Frame.Navigate(typeof(SetupUi));
		}

		private async void CreateCollections(User User)
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
		private async void CreateNewSettings(User user)
		{
			try
			{
				SettingsActions settingsActions = new(user.Username);
				string settingsPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, user.Username, "Settings", "Settings.db");

				if (!File.Exists(settingsPath))
				{
					await settingsActions.SettingsContext.Database.MigrateAsync();
				}

				if (File.Exists(settingsPath))
				{
					if (await settingsActions.SettingsContext.Database.CanConnectAsync())
					{
						_ = await settingsActions.InsertUserSettingsAsync(AppService.AppSettings);
					}
				}
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
			}
			
		}
		private async Task InPrivateUser()
		{
			User newUser = new()
			{
				Id = Guid.NewGuid(),
				Username = "Private",
				IsFirstLaunch = true,
				UserSettings = null
			};

			AuthService.AddUser(newUser);
			await UserManager.CreateUserAsync(newUser.Username, null, null, await UserImageHelper.GetImageStreamAsync(new UserImageItem { ImagePath = $"ms-appx:///Riverside.Graphite.Assets/Assets/{selectedImageName}", Name = newUser.Username }));
			_ = AuthService.Authenticate(newUser.Username);
			UserFolderManager.CreateUserFolders(newUser);
			await CopyImageToUserDirectory(newUser);
			await UserCreateDatabase(newUser);

		}
		private async Task CreateUserOnStartup()
		{
			Riverside.Graphite.Core.User newUser = new()
			{
				Username = UserName.Text,
			};

			
			await UserManager.CreateUserAsync(newUser.Username, null, null, await UserImageHelper.GetImageStreamAsync(new UserImageItem { ImagePath = $"ms-appx:///Riverside.Graphite.Assets/Assets/{selectedImageName}", Name = newUser.Username }));
			UserFolderManager.CreateUserFolders(newUser);
			_ = AuthService.Authenticate(newUser.Username);

			await CopyImageToUserDirectory(newUser);
		
			await UserCreateDatabase(newUser);

		}

		async Task UserCreateDatabase(User user)
		{
				await databaseServices.DatabaseCreationValidation();
				CreateCollections(user);
				CreateNewSettings(user);
			
		}
		private async Task CopyImageToUserDirectory(Riverside.Graphite.Core.User user)
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