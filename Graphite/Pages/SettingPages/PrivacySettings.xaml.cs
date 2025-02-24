using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Storage;
using Graphite.UserSys;
using System.Threading.Tasks;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class PrivacySettings : Page
	{
		public PrivacySettingsViewModel ViewModel { get; set; }
		private string currentUsername;

		public PrivacySettings()
		{
			this.InitializeComponent();
			ViewModel = new PrivacySettingsViewModel();
			this.DataContext = ViewModel;
			InitializeSettings();
		}

		private async void InitializeSettings()
		{
			currentUsername = UserManager.GetCurrentUsername();

			DisableJavaScriptToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "DisableJavaScript", false);
			PasswordWebMessFillToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "DisablePasswordSaving", false);
			DisableWebMessFillToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "DisableWebMessaging", false);
			DisableGeneralAutoFillToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "DisableGeneralAutoFill", false);
		}

		private async void DisableJavaScriptToggle_Toggled(object sender, RoutedEventArgs e)
		{
			bool isEnabled = DisableJavaScriptToggle.IsOn;
			await SettingsManager.UpdateSettingAsync(currentUsername, "DisableJavaScript", isEnabled);
			ApplySetting("DisableJavaScript", isEnabled);
		}

		private async void PasswordWebMessFillToggle_Toggled(object sender, RoutedEventArgs e)
		{
			bool isEnabled = PasswordWebMessFillToggle.IsOn;
			await SettingsManager.UpdateSettingAsync(currentUsername, "DisablePasswordSaving", isEnabled);
			ApplySetting("DisablePasswordSaving", isEnabled);
		}

		private async void DisableWebMessFillToggle_Toggled(object sender, RoutedEventArgs e)
		{
			bool isEnabled = DisableWebMessFillToggle.IsOn;
			await SettingsManager.UpdateSettingAsync(currentUsername, "DisableWebMessaging", isEnabled);
			ApplySetting("DisableWebMessaging", isEnabled);
		}

		private async void DisableGeneralAutoFillToggle_Toggled(object sender, RoutedEventArgs e)
		{
			bool isEnabled = DisableGeneralAutoFillToggle.IsOn;
			await SettingsManager.UpdateSettingAsync(currentUsername, "DisableGeneralAutoFill", isEnabled);
			ApplySetting("DisableGeneralAutoFill", isEnabled);
		}

		private void ApplySetting(string settingName, bool isEnabled)
		{
			switch (settingName)
			{
				case "DisableJavaScript":
					// Apply JavaScript disabling logic
					// For example: WebView2.CoreWebView2.Settings.IsScriptEnabled = !isEnabled;
					break;
				case "DisablePasswordSaving":
					// Apply password autosave disabling logic
					// For example: WebView2.CoreWebView2.Settings.IsPasswordAutosaveEnabled = !isEnabled;
					break;
				case "DisableWebMessaging":
					// Apply web messages disabling logic
					// For example: WebView2.CoreWebView2.Settings.IsWebMessageEnabled = !isEnabled;
					break;
				case "DisableGeneralAutoFill":
					// Apply autofill disabling logic
					// For example: WebView2.CoreWebView2.Settings.IsGeneralAutofillEnabled = !isEnabled;
					break;
			}
		}

		private async void Permission_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggleSwitch && toggleSwitch.DataContext is PermissionItem permissionItem)
			{
				permissionItem.IsAllowed = toggleSwitch.IsOn;
				ViewModel.UpdatePermission(permissionItem);
				await SavePermissionsAsync();
			}
		}

		private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is PermissionItem permissionItem)
			{
				ViewModel.DeletePermission(permissionItem);
				await SavePermissionsAsync();
			}
		}

		private async Task SavePermissionsAsync()
		{
			// Save permissions to SettingsManager
			await SettingsManager.UpdateSettingAsync(currentUsername, "Permissions", ViewModel.Permissions);
		}
	}

	public class PrivacySettingsViewModel
	{
		public ObservableCollection<PermissionItem> Permissions { get; set; }
		public bool HasPermissions => Permissions.Count > 0;

		public PrivacySettingsViewModel()
		{
			Permissions = new ObservableCollection<PermissionItem>();
			LoadPermissions();
		}

		private async void LoadPermissions()
		{
			string currentUsername = UserManager.GetCurrentUsername();
			var savedPermissions = await SettingsManager.GetSettingAsync<ObservableCollection<PermissionItem>>(currentUsername, "Permissions");

			if (savedPermissions != null)
			{
				Permissions = savedPermissions;
			}
			else
			{
				// If no permissions are saved, add some default ones
				Permissions.Add(new PermissionItem { Url = "example.com", PermissionType = "Location", IsAllowed = true, Icon = "\uE1D2" });
				Permissions.Add(new PermissionItem { Url = "test.com", PermissionType = "Camera", IsAllowed = false, Icon = "\uE714" });
			}
		}

		public void UpdatePermission(PermissionItem permissionItem)
		{
			var index = Permissions.IndexOf(permissionItem);
			if (index != -1)
			{
				Permissions[index] = permissionItem;
			}
		}

		public void DeletePermission(PermissionItem permissionItem)
		{
			Permissions.Remove(permissionItem);
		}
	}

	public class PermissionItem
	{
		public string Url { get; set; }
		public string PermissionType { get; set; }
		public bool IsAllowed { get; set; }
		public string Icon { get; set; }
	}
}