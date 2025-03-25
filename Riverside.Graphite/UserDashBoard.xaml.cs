using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Microsoft.UI.Windowing;
using System.Net.Http;
using System.Text.Json;
using Windows.Devices.Geolocation;
using CommunityToolkit.Mvvm.ComponentModel;
using Riverside.Graphite.Core;
using System.Threading;
using Riverside.Graphite.Services;
using Riverside.Graphite;
using Riverside.Graphite.Services.Migration;
using Riverside.Graphite.Setup.OOBE;


namespace Riverside.Graphite
{
	public sealed partial class UserDashBoard : Window
	{
		public UserV2? AuthUser { get; set; }
		
		private AppWindow? appWindow;
		private readonly HttpClient _httpClient;
		private const string WEATHER_API_KEY = "39dd21e1ba6f4a748d5144656253101"; // Replace with your API key
		private bool _disposedValue;
        private readonly ObservableRecipient? _recipient;
		public UserDashBoardViewModel ViewModel { get; set; }	
		public CancellationToken CancellationToken { get; set; }
		public static UserDashBoard? Instance { get; set; }

		public UserDashBoard(ObservableRecipient recipient)
        {
            this.InitializeComponent();
            InitializeAsync();
            _httpClient = new HttpClient();
            _recipient = recipient;
        }
		public UserDashBoard()
		{
			this.InitializeComponent();
			Instance = this;
			ViewModel = new UserDashBoardViewModel();	
			ViewModel.ParentWindow = this;
			ViewModel.ParentGrid = UserListView; 
			InitializeAsync();
			this.Closed += (s, e) =>
			{
				CloseCancelToken(CancellationToken);
			};
			_httpClient = new HttpClient();
		}

		public Task CloseCancelToken(CancellationToken cancellationToken)
		{
			// need to assign reference token in order to cancel !
			CancellationTokenSource cancel = new();
			cancel.Cancel();
			CancellationToken = cancellationToken = cancel.Token;
			return Task.CompletedTask;
		}


		private async void InitializeAsync()
		 {
			await UserManager.InitializeAsync();
			await UserManager.ValidateSecurityDatabase(); // Validate the security database
			await LoadUsersAsync();
		}

		public  async Task LoadUsersAsync()
		{
			try
			{
				ObservableCollection<UserViewModel> Users = new ObservableCollection<UserViewModel>();
				string graphiteDataPath = UserManager.GraphiteDataPath;

				// Create the directory if it doesn't exist
				Directory.CreateDirectory(graphiteDataPath);
				var users = await UserManager.GetAllUsersAsync();
				foreach (var user in users)
				{
					var profileImagePath = Path.Combine(graphiteDataPath, user.Username, "profile_image.jpg");
					var userViewModel = new UserViewModel
					{
						Username = user.Username,
						ProfileImageSource = await LoadProfileImageAsync(profileImagePath)
					};
					Users.Add(userViewModel);
				}

				ViewModel.Users = Users;
				ViewModel.RaisePropertyChanges(nameof(ViewModel.Users));	

				// Show or hide the NoUsersGrid based on whether there are any users
				NoUsersGrid.Visibility = ViewModel.Users.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				UserListView.Visibility = ViewModel.Users.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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

		private async void UserListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is UserViewModel selectedUser)
			{
				UserManager.ActiveElement = (sender as UIElement);	
				await AttemptLoginAsync(selectedUser.Username);
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
						AuthUser = authenticatedUser;
						if (AuthService.CurrentUser?.Username != authenticatedUser.Username  && AuthService.CurrentUser is not null)
							await Windows.System.Launcher.LaunchUriAsync(new System.Uri($"firebrowseruser://{authenticatedUser.Username}"));


						this.Close();
						// Close the login window
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

		private async Task ShowPasswordDialogAsync(UserV2 user)
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
					// open new window for the authenticated user
					if (AuthService.CurrentUser is not null)
					{
						if (AuthService.CurrentUser.Username != authenticatedUser.Username)
						{
							AuthService.Authenticate(authenticatedUser.Username);	

							await Windows.System.Launcher.LaunchUriAsync(new System.Uri($"firebrowseruser://{authenticatedUser.Username}"));
						}
					}

					// set the authenticated user back to riverside.graphite.Appservice

					AuthUser = authenticatedUser;
					//AuthService.Authenticate(AuthUser?.Username); 
					this.Close(); // Close the login window
				}
				else
				{
					await ShowErrorMessageAsync("Invalid password.");
				}
			}
		}

		private async Task ShowErrorMessageAsync(string message, string title = null)
		{
			var dialog = new ContentDialog
			{
				Title = title ?? "Error",
				Content = message,
				CloseButtonText = "OK",
				XamlRoot = Content.XamlRoot
			};
			await dialog.ShowAsync();
		}

		private async void CreateNewUser_Click(object sender, RoutedEventArgs e)
		{
			SetupWelcome setupWelcome = new SetupWelcome();
			await AppService.ConfigureSettingsWindow(setupWelcome); 

			setupWelcome.Activate();
			
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button deleteButton && deleteButton.DataContext is UserViewModel vm)
			{
				await Task.Delay(100);	
				UserManager.ActiveElement = (sender as UIElement);

				if(await UserManager.DeleteUserAsync(vm.Username))
					NotificationQueue.Show("User deleted successfully", TimeSpan.FromSeconds(3).Seconds, "Graphite Users");

				await LoadUsersAsync();	
			}
		}

		private async void OpenGuestUser_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var guestUser = await UserManager.LoginAsGuestAsync();
				// Navigate to the main application window or page for the guest user
				AuthUser = guestUser;
				this.Close();
			}
			catch (Exception ex)
			{
				await ShowErrorMessageAsync($"Failed to login as Guest: {ex.Message}");
			}
		}

		private async Task UpdateWeatherAsync()
		{
			try
			{
				// Get location
				var location = await GetLocationAsync();
				Loading.Visibility = Visibility.Visible;
				// Call weather API
				using (var response = await _httpClient.GetAsync(
					$"http://api.weatherapi.com/v1/current.json?key={WEATHER_API_KEY}&q={location.Latitude},{location.Longitude}"))
				{
					response.EnsureSuccessStatusCode();
					var json = await response.Content.ReadAsStringAsync();
					using (var weatherData = JsonDocument.Parse(json))
					{
						var current = weatherData.RootElement.GetProperty("current");
						var temp = current.GetProperty("temp_c").GetDouble();
						var conditionCode = current.GetProperty("condition").GetProperty("code").GetInt32();
						var conditionText = current.GetProperty("condition").GetProperty("text").GetString();

						// Update UI elements
						WeatherIcon.Glyph = GetWeatherIconGlyph(conditionCode);
						WeatherSummaryText.Text = $"{Math.Round(temp)}°C";

						// Update flyout content
						WeatherLocationText.Text = $"{weatherData.RootElement.GetProperty("location").GetProperty("name").GetString()}, {weatherData.RootElement.GetProperty("location").GetProperty("country").GetString()}";
						WeatherDescriptionText.Text = conditionText;
						WeatherTemperatureText.Text = $"{Math.Round(temp)}°C";
						WeatherHumidityText.Text = $"Humidity: {current.GetProperty("humidity").GetInt32()}%";
						WeatherWindText.Text = $"Wind: {current.GetProperty("wind_kph").GetDouble()} km/h";

						Loading.Visibility = Visibility.Collapsed;
					}
				}
			}
			catch (Exception ex)
			{
				// Handle error gracefully
				WeatherIcon.Glyph = "\uE9C8"; // Question mark icon
				WeatherSummaryText.Text = "Weather Unavailable";

				// Update flyout content to show error
				WeatherLocationText.Text = "Error";
				WeatherDescriptionText.Text = "Unable to fetch weather data";
				WeatherTemperatureText.Text = "--°C";
				WeatherHumidityText.Text = "Humidity: --";
				WeatherWindText.Text = "Wind: --";

				// Log the error
				System.Diagnostics.Debug.WriteLine($"Weather update failed: {ex.Message}");
			}
		}

		private string GetWeatherIconGlyph(int conditionCode)
		{
			// This is a simplified mapping. You might want to expand this based on the API's condition codes.
			return conditionCode switch
			{
				1000 => "\uE706", // Sunny
				1003 => "\uE753", // Partly cloudy
				1006 => "\uE9C0", // Cloudy
				1183 => "\uE9C4", // Light rain
				_ => "\uE9C8"     // Default/Unknown
			};
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

		private async void WeatherButton_Click(object sender, RoutedEventArgs e)
		{
			await UpdateWeatherAsync();
		}

		private async void StartMigration_Click(object sender, RoutedEventArgs e)
		{
			MigrationWindow ws = new MigrationWindow();
			await AppService.ConfigureSettingsWindow(ws);
			ws.Activate();
			
		}

		private async void MigrateSys_Click(object sender, RoutedEventArgs e)
		{
			MigrationWindow ws = new MigrationWindow();
			await AppService.ConfigureSettingsWindow(ws);
			ws.Activate();
			
		}

		private void ExitSys_Click(object sender, RoutedEventArgs e)
		{
			CloseCancelToken(CancellationToken);
			Application.Current.Exit();	

		}

		private async void BtnNewPassInput_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button deleteButton && deleteButton.DataContext is UserViewModel vm)
			{

				await Task.Delay(100);

				UserManager.ActiveElement = (sender as UIElement);
				var user = await UserManager.GetUserAsync(vm.Username);

				if (user != null)
				{
					if (user.HasPassword)
					{
						if (!await UserManager.ValidatePassWord(user))
						{
							await ShowErrorMessageAsync("Invalid password");	
						}
						else {

							if (await UserManager.MigrateUserToNewPassword(user))
								await ShowErrorMessageAsync("Password is saved !", "Success");
						}
					}
					else
						if (await UserManager.MigrateUserToNewPassword(user))
							await ShowErrorMessageAsync("Password is saved !", "Success");
						



				}

				await LoadUsersAsync();
			}
		}
    }

	public class UserViewModel
	{
		public string Username { get; set; }
		public string Email { get; set; }
		public BitmapImage ProfileImageSource { get; set; }
	}
}

