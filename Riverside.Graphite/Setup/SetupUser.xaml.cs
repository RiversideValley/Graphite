using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Assets;
using Riverside.Graphite.Core;
using Riverside.Graphite.Core.Helper;
using Riverside.Graphite.Core.Models;
using Riverside.Graphite.Services;
using Riverside.Graphite.Setup.UserCreateFunctions;
using System;
using System.Threading.Tasks;

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
			if (AuthService.UserExists(UserName.Text) is User)
			{
				NotificationQueue.Show("User already exists\nPlease choose a different username", 2000, "User Creation");
				UserName.Text = string.Empty;
				return;
			}

			await UserManager.InitializeAsync(); // This is a static method, so it should be called on the class, not an instance
			await CreateUserAndNavigate();
		}

		private async Task CreateUserAndNavigate()
		{
			await CreateUserOnStartup();
			await InPrivateUser();
			_ = Frame.Navigate(typeof(SetupUi));
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

			await UserManager.CreateUserAsync(newUser.Username, null, null, await UserImageHelper.GetImageStreamAsync(new UserImageItem { ImagePath = $"ms-appx:///Riverside.Graphite.Assets/Assets/{selectedImageName}", Name = newUser.Username }));
			_ = AuthService.Authenticate(newUser.Username);
			UserFolderManager.CreateUserFolders(newUser);
			await AddUserDefaults.CopyImageToUserDirectory(newUser, selectedImageName);
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

			await AddUserDefaults.CopyImageToUserDirectory(newUser, selectedImageName);

			await UserCreateDatabase(newUser);

		}

		async Task UserCreateDatabase(User user)
		{
			await databaseServices.DatabaseCreationValidation(user);
			AddUserDefaults.CreateCollections(user);
			AddUserDefaults.CreateNewSettings(user);
		}

	}
}