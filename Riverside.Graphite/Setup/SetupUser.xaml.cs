using Riverside.Graphite.Assets;
using Riverside.Graphite.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace FireBrowserWinUi3
{
	public sealed partial class SetupUser : Page
	{
		private string selectedImageName = "clippy.png";

		public SetupUser()
		{
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

		private  async Task InPrivateUser()
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