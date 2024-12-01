using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Services;
using Riverside.Graphite.Runtime.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
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
				Debug.WriteLine("Initializing SettingsPrivacy page");
				SettingsService = App.GetService<SettingsService>();
				ViewModel = new SettingsPrivacyViewModel();
				InitializeComponent();
				LoadSettings();
				LoadPermissions();
				Debug.WriteLine("SettingsPrivacy page initialized successfully");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error initializing SettingsPrivacy page: {ex.Message}");
			}
		}

		private void LoadSettings()
		{
			try
			{
				Debug.WriteLine("Loading settings");
				DisableJavaScriptToggle.IsOn = SettingsService.CoreSettings.DisableJavaScript;
				DisablWebMessFillToggle.IsOn = SettingsService.CoreSettings.DisableWebMess;
				DisableGenaralAutoFillToggle.IsOn = SettingsService.CoreSettings.DisableGenAutoFill;
				PasswordWebMessFillToggle.IsOn = SettingsService.CoreSettings.DisablePassSave;
				Debug.WriteLine("Settings loaded successfully");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading settings: {ex.Message}");
			}
		}

		private async void LoadPermissions()
		{
			try
			{
				Debug.WriteLine("Loading permissions");
				string username = AuthService.CurrentUser?.Username ?? "default";
				await PermissionManager.LoadPermissionsAsync(username);
				var permissions = await PermissionManager.GetAllPermissionsAsync(username);

				foreach (var permission in permissions)
				{
					ViewModel.Permissions.Add(new PermissionItem
					{
						Url = permission.Key.Split(':')[0],
						PermissionType = permission.Key.Split(':')[1],
						IsAllowed = permission.Value.State == CoreWebView2PermissionState.Allow
					});
				}

				ViewModel.UpdatePermissionVisibility();
				Debug.WriteLine($"Loaded {ViewModel.Permissions.Count} permissions");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading permissions: {ex.Message}");
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
					Debug.WriteLine($"Toggling setting: {settingName} to {value}");

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
					Debug.WriteLine("Settings saved successfully");
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
					Debug.WriteLine($"Toggling permission: {permission.PermissionType} for {permission.Url}");
					if (Enum.TryParse(permission.PermissionType, out CoreWebView2PermissionKind permissionKind))
					{
						var username = AuthService.CurrentUser?.Username ?? "default";
						await PermissionManager.UpdatePermission(
							username,
							permission.Url,
							permissionKind,
							toggleSwitch.IsOn);
						Debug.WriteLine("Permission updated successfully");
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

		public bool HasNoPermissions => !HasPermissions;

		public ObservableCollection<PermissionItem> Permissions { get; } = new ObservableCollection<PermissionItem>();

		public void UpdatePermissionVisibility()
		{
			HasPermissions = Permissions.Any();
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
