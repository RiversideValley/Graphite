using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Assets;
using Riverside.Graphite.Core;
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
			await CreateUserAndNavigate();
		}

		private async Task CreateUserAndNavigate()
		{
			await CreateUserOnStartup();
			await InPrivateUser();
			_ = Frame.Navigate(typeof(SetupUi));
		}

		private async void CreateCollections()
		{

			try
			{
				HistoryActions historyActions = new HistoryActions(AuthService.NewCreatedUser?.Username);
				string historyPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser?.Username, "Database", "History.db");
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
		private async void CreateNewSettings()
		{
			try
			{
				SettingsActions settingsActions = new(AuthService.NewCreatedUser?.Username);
				string settingsPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser?.Username, "Settings", "Settings.db");

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
			finally
			{
				AuthService.NewCreatedUser = null;
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
			UserFolderManager.CreateUserFolders(newUser);
			AuthService.CurrentUser.Username = newUser.Username;
			_ = AuthService.Authenticate(newUser.Username);
			await CopyImageToUserDirectory(newUser);
			await UserCreateDatabase();

		}
		private async Task CreateUserOnStartup()
		{
			Riverside.Graphite.Core.User newUser = new()
			{
				Username = UserName.Text,
			};

			List<Riverside.Graphite.Core.User> users = new() { newUser };
			UserFolderManager.CreateUserFolders(newUser);
			UserDataManager.SaveUsers(users);
			AuthService.AddUser(newUser);
			_ = AuthService.Authenticate(newUser.Username);

			await CopyImageToUserDirectory(newUser);

			await UserCreateDatabase();

		}

		async Task UserCreateDatabase()
		{
			if (AuthService.IsUserAuthenticated)
			{
				_ = await databaseServices.DatabaseCreationValidation();
				CreateCollections();
				CreateNewSettings();
			}
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