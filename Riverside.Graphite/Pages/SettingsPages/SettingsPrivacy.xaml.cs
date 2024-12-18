using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Riverside.Graphite.Core;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Riverside.Graphite.Pages.SettingsPages
{
	public sealed partial class SettingsPrivacy : Page
	{
		private SettingsService SettingsService { get; set; }
		public SettingsPrivacyViewModel ViewModel { get; set; }

		public SettingsPrivacy()
		{
			try
			{
				SettingsService = App.GetService<SettingsService>();
				ViewModel = new SettingsPrivacyViewModel();
				InitializeComponent();
				LoadSettings();
				_ = InitializeAsync(); // Call asynchronously
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error initializing SettingsPrivacy page: {ex.Message}");
			}
		}

		private async Task InitializeAsync()
		{
			await LoadPermissionsFromJsonAsync();
			// Force UI update
			ViewModel.UpdatePermissionVisibility();
		}

		private void LoadSettings()
		{
			try
			{
				DisableJavaScriptToggle.IsOn = SettingsService.CoreSettings.DisableJavaScript;
				DisablWebMessFillToggle.IsOn = SettingsService.CoreSettings.DisableWebMess;
				DisableGenaralAutoFillToggle.IsOn = SettingsService.CoreSettings.DisableGenAutoFill;
				PasswordWebMessFillToggle.IsOn = SettingsService.CoreSettings.DisablePassSave;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading settings: {ex.Message}");
			}
		}

		private async Task LoadPermissionsFromJsonAsync()
		{
			try
			{
				string filePath = Path.Combine(UserDataManager.CoreFolderPath, "Users", AuthService.CurrentUser.Username, "Permissions", "sitepermissions.json");


				if (File.Exists(filePath))
				{
					string json = await File.ReadAllTextAsync(filePath);

					var jsonObject = JObject.Parse(json);

					ViewModel.Permissions.Clear();
					foreach (var property in jsonObject.Properties())
					{
						var parts = property.Name.Split(':');
						if (parts.Length == 2)
						{
							var permissionData = property.Value.ToObject<PermissionManager.PermissionItem>();
							ViewModel.Permissions.Add(new PermissionItem
							{
								Url = parts[0],
								PermissionType = parts[1],
								IsAllowed = permissionData.State == CoreWebView2PermissionState.Allow
							});
						}
					}

					ViewModel.UpdatePermissionVisibility();
				}
				else
				{
					Debug.WriteLine("sitepermissions.json file not found");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading permissions from JSON: {ex.Message}");
				Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			}
		}

		private async void ToggleSetting_Toggled(object sender, RoutedEventArgs e)
		{
			try
			{
				if (sender is ToggleSwitch toggleSwitch)
				{
					bool value = toggleSwitch.IsOn;
					string settingName = toggleSwitch.Name;

					switch (settingName)
					{
						case nameof(DisableJavaScriptToggle):
							SettingsService.CoreSettings.DisableJavaScript = value;
							break;
						case nameof(DisableGenaralAutoFillToggle):
							SettingsService.CoreSettings.DisableGenAutoFill = value;
							break;
						case nameof(DisablWebMessFillToggle):
							SettingsService.CoreSettings.DisableWebMess = value;
							break;
						case nameof(PasswordWebMessFillToggle):
							SettingsService.CoreSettings.DisablePassSave = value;
							break;
						default:
							Debug.WriteLine($"Unknown setting name: {settingName}");
							return;
					}

					await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error toggling setting: {ex.Message}");
			}
		}

		private async void Permission_Toggled(object sender, RoutedEventArgs e)
		{
			try
			{
				if (sender is ToggleSwitch toggleSwitch && toggleSwitch.DataContext is PermissionItem permission)
				{
					if (Enum.TryParse(permission.PermissionType, out CoreWebView2PermissionKind permissionKind))
					{
						var username = AuthService.CurrentUser?.Username ?? "default";
						bool isAllowed = toggleSwitch.IsOn;
						await PermissionManager.UpdatePermission(
							username,
							permission.Url,
							permissionKind,
							isAllowed);

						permission.IsAllowed = isAllowed;


						// Save updated permissions to JSON file
						await SavePermissionsToJsonAsync();
					}
					else
					{
						Debug.WriteLine($"Failed to parse permission kind: {permission.PermissionType}");
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error toggling permission: {ex.Message}");
			}
		}

		private async Task SavePermissionsToJsonAsync()
		{
			try
			{
				string filePath = Path.Combine(UserDataManager.CoreFolderPath, "permission", "sitepermissions.json");

				var permissions = ViewModel.Permissions.ToDictionary(
					p => $"{p.Url}:{p.PermissionType}",
					p => new PermissionManager.PermissionItem
					{
						State = p.IsAllowed ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny,
						Kind = Enum.Parse<CoreWebView2PermissionKind>(p.PermissionType),
						Changed = true,
						LastUpdated = DateTime.UtcNow
					}
				);

				string json = JsonConvert.SerializeObject(permissions, Formatting.Indented);

				// Ensure the directory exists
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));

				await File.WriteAllTextAsync(filePath, json);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error saving permissions to JSON: {ex.Message}");
			}
		}

		private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is PermissionItem selectedItem)
			{
				//PermissionManager.DeletePermission(AuthService.CurrentUser.Username, selectedItem.Url, selectedItem.Kind);
				//PermissionManager.LoadPermissionsAsync(AuthService.CurrentUser.Username); // Refresh the permissions list
			}
		}
	}

	public class SettingsPrivacyViewModel : INotifyPropertyChanged
	{
		private bool _hasPermissions;
		public bool HasPermissions
		{
			get => _hasPermissions;
			set
			{
				if (_hasPermissions != value)
				{
					_hasPermissions = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(HasNoPermissions));
				}
			}
		}

		private ObservableCollection<PermissionItem> _permissions;
		public ObservableCollection<PermissionItem> Permissions
		{
			get => _permissions ?? (_permissions = new ObservableCollection<PermissionItem>());
			set
			{
				if (_permissions != value)
				{
					_permissions = value;
					OnPropertyChanged();
				}
			}
		}

		public bool HasNoPermissions => !HasPermissions;

		public void UpdatePermissionVisibility()
		{
			HasPermissions = Permissions.Any();
			OnPropertyChanged(nameof(Permissions));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class PermissionItem : INotifyPropertyChanged
	{
		private string _url;
		public string Url
		{
			get => _url;
			set
			{
				if (_url != value)
				{
					_url = value;
					OnPropertyChanged();
				}
			}
		}

		private string _permissionType;
		public string PermissionType
		{
			get => _permissionType;
			set
			{
				if (_permissionType != value)
				{
					_permissionType = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(Icon));
				}
			}
		}

		private bool _isAllowed;
		public bool IsAllowed
		{
			get => _isAllowed;
			set
			{
				if (_isAllowed != value)
				{
					_isAllowed = value;
					OnPropertyChanged();
				}
			}
		}

		public string Icon
		{
			get
			{
				return PermissionType switch
				{
					nameof(CoreWebView2PermissionKind.Microphone) => "\uE720",
					nameof(CoreWebView2PermissionKind.Camera) => "\uE722",
					nameof(CoreWebView2PermissionKind.Geolocation) => "\uE819",
					nameof(CoreWebView2PermissionKind.Notifications) => "\uE91C",
					nameof(CoreWebView2PermissionKind.OtherSensors) => "\uE7F8",
					nameof(CoreWebView2PermissionKind.ClipboardRead) => "\uE8C8",
					nameof(CoreWebView2PermissionKind.MultipleAutomaticDownloads) => "\uE896",
					nameof(CoreWebView2PermissionKind.Autoplay) => "\uE768",
					nameof(CoreWebView2PermissionKind.WindowManagement) => "\uE87B",



					_ => "\uE783",
				};
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

