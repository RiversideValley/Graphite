using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Graphite.UserSys;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class GeneralSettings : Page
	{
		private ObservableCollection<UserViewModel> Users { get; set; }

		public GeneralSettings()
		{
			this.InitializeComponent();
			Users = new ObservableCollection<UserViewModel>();
			UserListView.ItemsSource = Users;
			Loaded += GeneralSettings_Loaded;
		}

		private async void GeneralSettings_Loaded(object sender, RoutedEventArgs e)
		{
			await InitializeAsync();
			await LoadUsersAsync();
		}

		private async Task InitializeAsync()
		{
			string currentUsername = UserManager.GetCurrentUsername();
			if (!string.IsNullOrEmpty(currentUsername))
			{
				User.Text = currentUsername;
				await LoadProfilePictureAsync(currentUsername);
			}
			else
			{
				User.Text = "No user logged in";
			}
		}

		private async Task LoadProfilePictureAsync(string username)
		{
			try
			{
				string profileImagePath = await UserManager.GetProfileImagePathAsync(username);
				if (!string.IsNullOrEmpty(profileImagePath))
				{
					StorageFile file = await StorageFile.GetFileFromPathAsync(profileImagePath);
					using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
					{
						BitmapImage bitmapImage = new BitmapImage();
						await bitmapImage.SetSourceAsync(fileStream);
						PersonImage.ProfilePicture = bitmapImage;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
				// Consider setting a default image here
			}
		}

		private async Task LoadUsersAsync()
		{
			try
			{
				string currentUsername = UserManager.GetCurrentUsername();
				var allUsers = await UserManager.GetAllUsersAsync();

				var otherUsers = allUsers.Where(u => u.Username != currentUsername).ToList();

				Users.Clear();

				if (otherUsers.Count == 0)
				{
					UserListView.Visibility = Visibility.Collapsed;
					NoUsersMessageGrid.Visibility = Visibility.Visible;
				}
				else
				{
					foreach (var user in otherUsers)
					{
						var profileImage = await LoadProfileImageAsync(user.ProfileImagePath);
						Users.Add(new UserViewModel
						{
							Username = user.Username,
							ProfileImage = profileImage
						});
					}
					UserListView.Visibility = Visibility.Visible;
					NoUsersMessageGrid.Visibility = Visibility.Collapsed;
				}
			}
			catch (Exception ex)
			{
				await ShowErrorDialogAsync("Failed to load users", ex.Message);
			}
		}

		private async Task<BitmapImage> LoadProfileImageAsync(string profileImagePath)
		{
			try
			{
				if (!string.IsNullOrEmpty(profileImagePath))
				{
					StorageFile file = await StorageFile.GetFileFromPathAsync(profileImagePath);
					using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
					{
						BitmapImage bitmapImage = new BitmapImage();
						await bitmapImage.SetSourceAsync(fileStream);
						return bitmapImage;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
			}
			return null;
		}

		private async void Switch_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var user = (UserViewModel)button.DataContext;
			try
			{
				await UserManager.SwitchProfileAsync(UserManager.GetCurrentUsername(), user.Username);
				await ShowInfoDialogAsync("Profile Switched", $"Switched to profile: {user.Username}");
				await InitializeAsync(); // Refresh current user info
				await LoadUsersAsync(); // Refresh user list
			}
			catch (Exception ex)
			{
				await ShowErrorDialogAsync("Failed to switch profile", ex.Message);
			}
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var user = (UserViewModel)button.DataContext;

			ContentDialogResult result = await ShowConfirmationDialogAsync("Delete User", $"Are you sure you want to delete the user '{user.Username}'?");

			if (result == ContentDialogResult.Primary)
			{
				try
				{
					await UserManager.DeleteUserAsync(user.Username);
					Users.Remove(user);

					if (Users.Count == 0)
					{
						UserListView.Visibility = Visibility.Collapsed;
						NoUsersMessageGrid.Visibility = Visibility.Visible;
					}

					await ShowInfoDialogAsync("User Deleted", $"User '{user.Username}' has been deleted successfully.");
				}
				catch (Exception ex)
				{
					await ShowErrorDialogAsync("Failed to delete user", ex.Message);
				}
			}
		}

		private async Task ShowErrorDialogAsync(string title, string message)
		{
			ContentDialog errorDialog = new ContentDialog
			{
				Title = title,
				Content = message,
				CloseButtonText = "OK"
			};
			errorDialog.XamlRoot = this.XamlRoot;
			await errorDialog.ShowAsync();
		}

		private async Task ShowInfoDialogAsync(string title, string message)
		{
			ContentDialog infoDialog = new ContentDialog
			{
				Title = title,
				Content = message,
				CloseButtonText = "OK"
			};
			infoDialog.XamlRoot = this.XamlRoot;
			await infoDialog.ShowAsync();
		}

		private async Task<ContentDialogResult> ShowConfirmationDialogAsync(string title, string message)
		{
			ContentDialog confirmDialog = new ContentDialog
			{
				Title = title,
				Content = message,
				PrimaryButtonText = "Yes",
				CloseButtonText = "No"
			};
			confirmDialog.XamlRoot = this.XamlRoot;
			return await confirmDialog.ShowAsync();
		}
	}

	public class UserViewModel
	{
		public string Username { get; set; }
		public BitmapImage ProfileImage { get; set; }
	}
}