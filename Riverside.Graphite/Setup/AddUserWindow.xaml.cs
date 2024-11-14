using Riverside.Graphite.Core;
using Riverside.Graphite.Core.Helper;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Pages.SettingsPages;
using Riverside.Graphite.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace Riverside.Graphite
{
	public sealed partial class AddUserWindow : Window
	{
		public AddUserWindow()
		{
			InitializeComponent();
			ConfigureWindowAppearance(this);
			Activated += (s, e) => { _ = Userbox.Focus(FocusState.Programmatic); };
			Closed += async (s, e) =>
			{

				await Task.Delay(420);

				IntPtr ucHwnd = Windowing.FindWindow(null, nameof(UserCentral));
				if (ucHwnd != IntPtr.Zero)
				{
					Windowing.Center(ucHwnd);
					_ = Windowing.UpdateWindow(ucHwnd);
				}
				else
				{
					_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
				}
			};
		}

		private static void ConfigureWindowAppearance(Window wdn)
		{
			IntPtr hWnd = WindowNative.GetWindowHandle(wdn);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			AppWindow appWindow = AppWindow.GetFromWindowId(wndId);
			if (appWindow != null)
			{
				// Set the maximum size to 430 width and 612 height
				appWindow.SetPresenter(AppWindowPresenterKind.Default);
				OverlappedPresenter presenter = appWindow.Presenter as OverlappedPresenter;
				if (presenter != null)
				{
					presenter.IsResizable = true;
					presenter.IsMaximizable = false;
					presenter.IsModal = false;
				}
				appWindow.MoveAndResize(new RectInt32(600, 600, 430, 612));
				appWindow.SetPresenter(AppWindowPresenterKind.Default);

				appWindow.MoveInZOrderAtTop();
				appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
				appWindow.Title = "Create New User";
				appWindow.SetIcon("LogoSetup.ico");
				AppWindowTitleBar titleBar = appWindow.TitleBar;
				Windows.UI.Color btnColor = Colors.Transparent;
				titleBar.BackgroundColor = btnColor;
				titleBar.ForegroundColor = btnColor;
				titleBar.ButtonBackgroundColor = btnColor;
				titleBar.BackgroundColor = btnColor;
				titleBar.ButtonInactiveBackgroundColor = btnColor;
			}
		}

		private string iImage = "";
		public async Task CopyImageAsync(string iImage, string destinationFolderPath)
		{
			ImageHelper imgLoader = new();
			imgLoader.ImageName = iImage;
			_ = imgLoader.LoadImage($"{iImage}");

			StorageFolder destinationFolder = await StorageFolder.GetFolderFromPathAsync(destinationFolderPath);

			StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Riverside.Graphite.Core/Assets/{iImage}"));
			_ = await imageFile.CopyAsync(destinationFolder, "profile_image.jpg", NameCollisionOption.ReplaceExisting);

			Console.WriteLine("Image copied successfully!");
		}

		private void ProfileImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ProfileImage.SelectedItem != null)
			{
				string userImageName = ProfileImage.SelectedItem.ToString() + ".png";

				iImage = userImageName;
				ImageHelper imgLoader = new();
				Microsoft.UI.Xaml.Media.Imaging.BitmapImage userProfilePicture = imgLoader.LoadImage(userImageName);
				Pimg.ProfilePicture = userProfilePicture;
			}
		}

		private void Userbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UsrBox.Text = Userbox.Text;
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			string enteredUsername = Userbox.Text;

			if (string.IsNullOrWhiteSpace(enteredUsername))
			{
				return;
			}

			User newUser = new()
			{
				Id = Guid.NewGuid(),
				Username = enteredUsername,
				IsFirstLaunch = false,
				UserSettings = null
			};

			List<Riverside.Graphite.Core.User> users = new();
			users.Add(newUser);

			AuthService.AddUser(newUser);

			UserFolderManager.CreateUserFolders(newUser);

			string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, Userbox.Text.ToString());

			await CopyImageAsync(iImage.ToString(), destinationFolderPath);

			AuthService.NewCreatedUser = newUser;

			if (SettingsHome.Instance is not null)
			{
				_ = (SettingsHome.Instance?.LoadUsernames());
			}

			if (UserCentral.Instance is not null)
			{
				await UserCentral.Instance?.LoadDataGlobally();
			}


			AppService.CreateNewUsersSettings();
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			if (hWnd != IntPtr.Zero)
			{
				Windowing.HideWindow(hWnd);
			}


		}

		private void Button_Click_Close(object sender, RoutedEventArgs e)
		{

			IntPtr hUser = Windowing.FindWindow(null, nameof(UserCentral));
			if (hUser != IntPtr.Zero)
			{
				Windowing.Center(hUser);
			}

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			if (hWnd != IntPtr.Zero)
			{
				Windowing.HideWindow(hWnd);
			}

		}
	}
}