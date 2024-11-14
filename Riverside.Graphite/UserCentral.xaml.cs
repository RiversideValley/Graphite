using Riverside.Graphite.Core;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services;
using Riverside.Graphite.Services.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinRT.Interop;

namespace Riverside.Graphite
{

	public sealed partial class UserCentral : Window
	{
		public static UserCentral Instance { get; private set; }
		public UC_Viewmodel ViewModel { get; set; }
		public static bool IsOpen { get; private set; }
		public bool _isDataLoaded { get; set; }

		public UserCentral()
		{
			InitializeComponent();
			ViewModel = new UC_Viewmodel { ParentWindow = this, ParentGrid = GridUserCentral };
			Instance = this;
			UserListView.Loaded += async (_, _) =>
			{
				await LoadDataGlobally();
			};
			// Get the AppWindow for this window
			_ = Windowing.DialogWindow(this).ConfigureAwait(false);


		}


		public async Task LoadDataGlobally()
		{
			string coreFolderPath = UserDataManager.CoreFolderPath;
			ViewModel.Users = await GetUsernameFromCoreFolderPath(coreFolderPath);
			UserListView.ItemsSource = ViewModel.Users;
			ViewModel.RaisePropertyChanges(nameof(ViewModel.Users));
		}

		private async Task<List<UserExtend>> GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
		{
			try
			{
				string usrCoreFilePath = Path.Combine(coreFolderPath, "UsrCore.json");
				if (File.Exists(usrCoreFilePath))
				{
					string jsonContent = await File.ReadAllTextAsync(usrCoreFilePath);
					List<User> users = JsonSerializer.Deserialize<List<User>>(jsonContent);

					return users?.FindAll(user => !user.Username.Contains("Private"))?.ConvertAll(user => new UserExtend(user));
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading UsrCore.json: {ex.Message}");
			}
			return new List<UserExtend>();
		}

		private void UserListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0 && UserListView.SelectedItem is UserExtend selectedUser)
			{
				ViewModel.User = selectedUser;
				ViewModel.RaisePropertyChanges(nameof(ViewModel.User));

				if (AuthService.users.Count == 0)
				{
					AuthService.users = AuthService.LoadUsersFromJson();
				}

				_ = AuthService.Authenticate(selectedUser.FireUser.Username);
				// close active window if not Usercentral, and then assign it as usercentral and close to give -> windowscontroller notification of close usercentral 
				AppService.ActiveWindow?.Close();
				AppService.ActiveWindow = this;
				AppService.ActiveWindow?.Close();
			}
		}

		private async void AppBarButton_Click(object sender, RoutedEventArgs e)
		{
			AddUserWindow usr = new();
			usr.Closed += (s, e) =>
			{
				AppService.ActiveWindow = this;
			};
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			if (hWnd != IntPtr.Zero)
			{
				_ = Windowing.SetWindowPos(hWnd, Windowing.HWND_BOTTOM, 0, 0, 0, 0, Windowing.SWP_NOSIZE);
			}
			await Windowing.DialogWindow(usr);
			Windowing.Center(usr);
			_ = Windowing.ShowWindow(WindowNative.GetWindowHandle(usr), Windowing.WindowShowStyle.SW_SHOWDEFAULT);


		}

		private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
		{
			if (AppWindow != null)
			{
				OverlappedPresenter presenter = AppWindow.Presenter as OverlappedPresenter;
				presenter?.Minimize();
			}
		}


	}
}