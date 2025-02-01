using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Graphite.UserSys;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using System.Net.Http;
using System.Text.Json;
using Windows.Devices.Geolocation;
using Graphite.ViewModels;

namespace Graphite;
public sealed partial class MainWindow : Window
{
	private ObservableCollection<UserViewModel> Users { get; set; }
	private AppWindow appWindow;
	private readonly DispatcherTimer _weatherTimer;
	private readonly HttpClient _httpClient;
	private const string WEATHER_API_KEY = "39dd21e1ba6f4a748d5144656253101"; // Replace with your API key


	public MainWindow()
	{
		this.InitializeComponent();
		InitializeAsync();
		TitleTop();

		_httpClient = new HttpClient();

		UpdateWeather();
		_weatherTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMinutes(30)
		};
		_weatherTimer.Tick += WeatherTimer_Tick;
		_weatherTimer.Start();
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

	private async void InitializeAsync()
	{
		await UserManager.InitializeAsync();
		await LoadUsersAsync();
	}

	private async Task LoadUsersAsync()
	{
		try
		{
			Users = new ObservableCollection<UserViewModel>();
			string graphiteDataPath = UserManager.GraphiteDataPath;

			// Create the directory if it doesn't exist
			Directory.CreateDirectory(graphiteDataPath);

			// Get all user directories
			var userDirs = Directory.GetDirectories(graphiteDataPath);
			foreach (var userDir in userDirs)
			{
				var username = Path.GetFileName(userDir);
				if (!username.Equals("Guest", StringComparison.OrdinalIgnoreCase))
				{
					var profileImagePath = Path.Combine(userDir, "profile_image.jpg");

					var userViewModel = new UserViewModel
					{
						Username = username,
						ProfileImageSource = await LoadProfileImageAsync(profileImagePath)
					};

					Users.Add(userViewModel);
				}
			}

			UserListView.ItemsSource = Users;
		}
		catch (Exception ex)
		{
			await ShowErrorMessageAsync($"Failed to load users: {ex.Message}");
		}
	}

	private async Task<BitmapImage> LoadProfileImageAsync(string imagePath)
	{
		try
		{
			var bitmap = new BitmapImage();
			if (File.Exists(imagePath))
			{
				using (var stream = File.OpenRead(imagePath))
				{
					using (var randomAccessStream = new InMemoryRandomAccessStream())
					{
						await RandomAccessStream.CopyAsync(stream.AsInputStream(), randomAccessStream);
						randomAccessStream.Seek(0);
						await bitmap.SetSourceAsync(randomAccessStream);
					}
				}
			}
			return bitmap;
		}
		catch
		{
			return new BitmapImage();
		}
	}

	private void UserListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is UserViewModel selectedUser)
		{
			AttemptLoginAsync(selectedUser.Username);
		}
	}

	private async Task AttemptLoginAsync(string username)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				await ShowErrorMessageAsync("Please enter a username.");
				return;
			}

			var user = await UserManager.GetUserAsync(username);
			if (user == null)
			{
				await ShowErrorMessageAsync("User not found.");
				return;
			}

			if (user.HasPassword)
			{
				await ShowPasswordDialogAsync(user);
			}
			else
			{
				var authenticatedUser = await UserManager.AuthenticateAsync(username, null);
				if (authenticatedUser != null)
				{
					// Open the Welcome window
					HomeWindow welcomeWindow = new HomeWindow(authenticatedUser);
					welcomeWindow.Activate();
					this.Close(); // Close the login window
				}
				else
				{
					await ShowErrorMessageAsync("Login failed.");
				}
			}
		}
		catch (Exception ex)
		{
			await ShowErrorMessageAsync($"Login failed: {ex.Message}");
		}
	}

	private async Task ShowPasswordDialogAsync(User user)
	{
		var passwordBox = new PasswordBox { PlaceholderText = "Enter password" };
		var dialog = new ContentDialog
		{
			Title = $"Enter password for {user.Username}",
			PrimaryButtonText = "Login",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
			Content = passwordBox,
			XamlRoot = Content.XamlRoot
		};

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary)
		{
			var authenticatedUser = await UserManager.AuthenticateAsync(user.Username, passwordBox.Password);
			if (authenticatedUser != null)
			{
				// Open the Welcome window
				HomeWindow welcomeWindow = new HomeWindow(authenticatedUser);
				welcomeWindow.Activate();
				this.Close(); // Close the login window
			}
			else
			{
				await ShowErrorMessageAsync("Invalid password.");
			}
		}
	}

	private async Task ShowErrorMessageAsync(string message)
	{
		var dialog = new ContentDialog
		{
			Title = "Error",
			Content = message,
			CloseButtonText = "OK",
			XamlRoot = Content.XamlRoot
		};
		await dialog.ShowAsync();
	}

	private void CreateNewUser_Click(object sender, RoutedEventArgs e)
	{
		Graphite.Setup.OOBE.SetupWelcome setupWelcome = new Setup.OOBE.SetupWelcome();
		setupWelcome.Activate();
		this.Close();
	}

	private async void OpenGuestUser_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			var guestUser = await UserManager.LoginAsGuestAsync();
			// Navigate to the main application window or page for the guest user
			HomeWindow welcome = new HomeWindow(guestUser);
			welcome.Activate();
			this.Close();
		}
		catch (Exception ex)
		{
			await ShowErrorMessageAsync($"Failed to login as Guest: {ex.Message}");
		}
	}

	// Weather-related methods
	private async void WeatherTimer_Tick(object sender, object e)
	{
		await UpdateWeather();
	}

	private async Task UpdateWeather()
	{
		try
		{
			// Update day
			DayText.Text = DateTime.Now.ToString("dddd");

			// Get location
			var location = await GetLocationAsync();

			// Call weather API
			var response = await _httpClient.GetAsync(
				$"http://api.weatherapi.com/v1/current.json?key={WEATHER_API_KEY}&q={location.Latitude},{location.Longitude}");

			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				var weatherData = JsonDocument.Parse(json);
				var current = weatherData.RootElement.GetProperty("current");
				var temp = current.GetProperty("temp_c").GetDouble();
				var conditionCode = current.GetProperty("condition").GetProperty("code").GetInt32();

				// Update temperature
				TemperatureText.Text = $"{Math.Round(temp)}°";

			}
		}
		catch (Exception ex)
		{
			// Handle error gracefully
			DayText.Text = DateTime.Now.ToString("dddd");
			TemperatureText.Text = "--°";
		}
	}

	private async Task<BasicGeoposition> GetLocationAsync()
	{
		var accessStatus = await Geolocator.RequestAccessAsync();
		if (accessStatus == GeolocationAccessStatus.Allowed)
		{
			var geolocator = new Geolocator { DesiredAccuracyInMeters = 1000 };
			var position = await geolocator.GetGeopositionAsync();
			return position.Coordinate.Point.Position;
		}
		else
		{
			// Default to a fixed location if permission is not granted
			return new BasicGeoposition { Latitude = 51.5074, Longitude = -0.1278 }; // London
		}
	}

	private void RestoreBackup_Click(object sender, RoutedEventArgs e)
	{

	}
}

public class UserViewModel
{
	public string Username { get; set; }
	public string Email { get; set; }
	public BitmapImage ProfileImageSource { get; set; }
}

