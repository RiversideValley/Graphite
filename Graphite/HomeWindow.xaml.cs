using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Graphite.Controls;
using Graphite.Helpers;
using Graphite.Pages;
using Graphite.UserSys;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT.Interop;
using static Graphite.Setup.OOBE.OOBEUser;

namespace Graphite
{
	public sealed partial class HomeWindow : Window
	{
		private User _currentUser;
		private AppWindow appWindow;
		private readonly int maxTabItems = 30;
		private TabManager _tabManager;
		private ObservableCollection<UserViewModel> Users { get; set; }
		private List<UserImageItem> userImages;

		public HomeWindow(User user)
		{
			this.InitializeComponent();
			_currentUser = user;
			this.Title = $"Graphite Home Page - {_currentUser.Username}";
			Users = new ObservableCollection<UserViewModel>();
			UserListView.ItemsSource = Users;

			if (QRCodeTypeComboBox.SelectedItem == null)
			{
				QRCodeTypeComboBox.SelectedIndex = 0;
			}
			UserName.Text = _currentUser.Username;
			UsernameDisplay.Text = _currentUser.Username;
			_tabManager = new TabManager(Tabs, _currentUser.Username);
			TitleTop();
			InitializeAsync();
			LoadUserImages();
			this.Closed += HomeWindow_Closed;
		}

		private async void LoadUserImages()
		{
			userImages = new List<UserImageItem>();
			var assetsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Graphite.UserSys", "Assets"));
			var imageFiles = await assetsFolder.GetFilesAsync();

			foreach (var file in imageFiles.Where(f => f.FileType == ".png" || f.FileType == ".jpg"))
			{
				userImages.Add(new UserImageItem
				{
					Name = Path.GetFileNameWithoutExtension(file.Name),
					ImagePath = $"ms-appx:///Graphite.UserSys/Assets/{file.Name}"
				});
			}

			pfpchanged.ItemsSource = userImages;

			// Load the last selected image for the current user
			var localSettings = ApplicationData.Current.LocalSettings;
			string currentUsername = UserManager.GetCurrentUsername();
			string settingsKey = $"{currentUsername}_SelectedProfileImage";

			if (localSettings.Values.TryGetValue(settingsKey, out object savedImagePath))
			{
				var selectedImage = userImages.FirstOrDefault(img => img.ImagePath == (string)savedImagePath);
				if (selectedImage != null)
				{
					pfpchanged.SelectedItem = selectedImage;
				}
				else if (userImages.Any())
				{
					pfpchanged.SelectedIndex = 0;
				}
			}
			else if (userImages.Any())
			{
				pfpchanged.SelectedIndex = 0;
			}
		}

		private void SaveSelectedProfileImage(string imagePath)
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			string currentUsername = UserManager.GetCurrentUsername();
			string settingsKey = $"{currentUsername}_SelectedProfileImage";
			localSettings.Values[settingsKey] = imagePath;
		}

		private async void InitializeAsync()
		{
			await StartupTabCheckAsync();
			await LoadProfilePictureAsync(_currentUser.Username);
			await LoadUsersAsync();
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
						MainUserPicture.ProfilePicture = bitmapImage;
						Profile.ProfilePicture = bitmapImage;
						RootImage.ProfilePicture = bitmapImage;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
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

				foreach (var user in otherUsers)
				{
					var profileImage = await LoadProfileImageAsync(user.ProfileImagePath);
					Users.Add(new UserViewModel
					{
						Username = user.Username,
						ProfileImageSource = profileImage
					});
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
			}
		}

		public async Task StartupTabCheckAsync()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			string cacheKey = $"{_currentUser.Username}_{TabManager.TabStateKey}";

			if (localSettings.Values.ContainsKey(cacheKey))
			{
				await _tabManager.RestoreTabsAsync(_currentUser.Username);
			}

			if (Tabs.TabItems.Count == 0)
			{
				_tabManager.CreateNewTab(typeof(NewTab));
			}

			await _tabManager.StartPreloadingTabs();
		}

		private void HomeWindow_Closed(object sender, WindowEventArgs args)
		{
			_tabManager.SaveTabStateAsync(_currentUser.Username.ToString());
		}

		public void TitleTop()
		{
			nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			appWindow = AppWindow.GetFromWindowId(windowId);
			appWindow.SetIcon("Logo.ico");

			if (!AppWindowTitleBar.IsCustomizationSupported())
			{
				throw new Exception("Unsupported OS version.");
			}

			AppWindowTitleBar titleBar = appWindow.TitleBar;
			titleBar.ExtendsContentIntoTitleBar = true;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = titleBar.ButtonBackgroundColor =
				titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor =
				titleBar.ButtonHoverBackgroundColor = btnColor;
		}

		private void Tabs_Loaded(object sender, RoutedEventArgs e)
		{
			Apptitlebar.SizeChanged += Apptitlebar_SizeChanged;
			Apptitlebar_LayoutUpdated(sender, e);
		}

		private void Apptitlebar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			try
			{
				double scaleAdjustment = GetScaleAdjustment();
				Apptitlebar.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
				Windows.Foundation.Point customDragRegionPosition = Apptitlebar.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

				Windows.Graphics.RectInt32[] dragRects = new Windows.Graphics.RectInt32[2];

				for (int i = 0; i < 2; i++)
				{
					dragRects[i] = new Windows.Graphics.RectInt32
					{
						X = (int)((customDragRegionPosition.X + (i * Apptitlebar.ActualWidth / 2)) * scaleAdjustment),
						Y = (int)(customDragRegionPosition.Y * scaleAdjustment),
						Height = (int)((Apptitlebar.ActualHeight - customDragRegionPosition.Y) * scaleAdjustment),
						Width = (int)(Apptitlebar.ActualWidth / 2 * scaleAdjustment)
					};
				}

				appWindow.TitleBar?.SetDragRectangles(dragRects);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error in Apptitlebar_SizeChanged: {ex.Message}");
			}
		}

		private void Apptitlebar_LayoutUpdated(object sender, object e)
		{
			double scaleAdjustment = GetScaleAdjustment();
			Apptitlebar.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
			Windows.Foundation.Point customDragRegionPosition = Apptitlebar.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

			List<Windows.Graphics.RectInt32> dragRectsList = new();

			for (int i = 0; i < 2; i++)
			{
				Windows.Graphics.RectInt32 dragRect = new()
				{
					X = (int)((customDragRegionPosition.X + (i * Apptitlebar.ActualWidth / 2)) * scaleAdjustment),
					Y = (int)(customDragRegionPosition.Y * scaleAdjustment),
					Height = (int)((Apptitlebar.ActualHeight - customDragRegionPosition.Y) * scaleAdjustment),
					Width = (int)(Apptitlebar.ActualWidth / 2 * scaleAdjustment)
				};

				dragRectsList.Add(dragRect);
			}

			Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

			appWindow.TitleBar?.SetDragRectangles(dragRects);
		}

		private void QRCodeTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (QRCodeTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
			{
				string selectedType = selectedItem.Content?.ToString() ?? string.Empty;

				if (WifiInputs != null)
					WifiInputs.Visibility = selectedType == "Wifi" ? Visibility.Visible : Visibility.Collapsed;

				if (TextInput != null)
					TextInput.Visibility = (selectedType == "Text" || selectedType == "URL") ? Visibility.Visible : Visibility.Collapsed;

				if (PhoneInput != null)
					PhoneInput.Visibility = selectedType == "Phone" ? Visibility.Visible : Visibility.Collapsed;

				if (CreateQRButton != null)
					CreateQRButton.Visibility = selectedType == "URL" ? Visibility.Collapsed : Visibility.Visible;

				if (selectedType == "URL" && TextInput != null)
				{
					TextInput.Visibility = Visibility.Collapsed;
				}
			}
		}

		private double GetScaleAdjustment()
		{
			nint hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
			nint hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

			_ = Windowing.GetDpiForMonitor(hMonitor, Windowing.Monitor_DPI_Type.MDT_Effective_DPI, out uint dpiX, out uint dpiY);

			double scaleX = dpiX / 96.0;
			double scaleY = dpiY / 96.0;

			return (scaleX + scaleY) / 2.0;
		}

		private void Tabs_AddTabButtonClick(TabView sender, object args)
		{
			if (sender.TabItems.Count < maxTabItems)
			{
				_tabManager.CreateNewTab(typeof(WebContent), null, false);
			}
		}

		private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			if (args.Tab is GraphiteTabViewItem tabToClose)
			{
				_tabManager.CloseTab(tabToClose);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow(_currentUser);
			settingsWindow.Activate();
		}

		private async void SwitchName_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var user = (UserViewModel)button.DataContext;
			try
			{
				await UserManager.SwitchProfileAsync(UserManager.GetCurrentUsername(), user.Username);
				_currentUser = await UserManager.GetUserAsync(user.Username);
				UserName.Text = _currentUser.Username;
				await LoadProfilePictureAsync(_currentUser.Username);
				await LoadUsersAsync();
				await _tabManager.RestoreTabsAsync(_currentUser.Username);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error switching profile: {ex.Message}");
				ContentDialog errorDialog = new ContentDialog
				{
					Title = "Error",
					Content = $"Failed to switch profile: {ex.Message}",
					CloseButtonText = "OK"
				};
				errorDialog.XamlRoot = this.Content.XamlRoot;
				await errorDialog.ShowAsync();
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

		private void MainUser_Click(object sender, RoutedEventArgs e)
		{
			UserFrame.Visibility = UserFrame?.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
		}

		private void Profile_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			Flyout flyout = ProfileCommander as Flyout;
			if (flyout != null)
			{
				flyout.ShowAt(sender as FrameworkElement);
			}
		}

		private async void pfpchanged_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (pfpchanged.SelectedItem is UserImageItem selectedItem)
			{
				string currentUsername = UserManager.GetCurrentUsername();
				if (string.IsNullOrEmpty(currentUsername))
				{
					// Handle the case where no user is logged in
					return;
				}

				try
				{
					// Source image path
					string sourceImagePath = selectedItem.ImagePath;

					// Get the source file
					StorageFile sourceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(sourceImagePath));

					// Destination path in the user's folder
					string userFolderPath = Path.Combine(UserManager.GraphiteDataPath, currentUsername);
					string destImagePath = Path.Combine(userFolderPath, "profile_image.jpg");

					// Ensure the user folder exists
					Directory.CreateDirectory(userFolderPath);

					// Copy the file
					StorageFile destFile = await StorageFile.GetFileFromPathAsync(destImagePath);
					await sourceFile.CopyAndReplaceAsync(destFile);

					// Update the database with the new image path
					await UserManager.UpdateProfileImageAsync(currentUsername, destImagePath);

					// Update the UI
					using (IRandomAccessStream fileStream = await destFile.OpenAsync(FileAccessMode.Read))
					{
						BitmapImage bitmapImage = new BitmapImage();
						await bitmapImage.SetSourceAsync(fileStream);

						RootImage.ProfilePicture = bitmapImage;
						MainUserPicture.ProfilePicture = bitmapImage;
						Profile.ProfilePicture = bitmapImage;
					}

					// Save the selected image path
					SaveSelectedProfileImage(selectedItem.ImagePath);
				}
				catch (Exception ex)
				{
					// Handle or log the error
					System.Diagnostics.Debug.WriteLine($"Error updating profile image: {ex.Message}");
				}
			}
		}

		public class UserImageItem
		{
			public string Name { get; set; }
			public string ImagePath { get; set; }
		}

		public class UserViewModel
		{
			public string Username { get; set; }
			public BitmapImage ProfileImageSource { get; set; }
		}
	}
}