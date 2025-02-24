using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class PrivacySettings : Page
	{
		public PrivacySettingsViewModel ViewModel { get; set; }

		public PrivacySettings()
		{
			this.InitializeComponent();
			ViewModel = new PrivacySettingsViewModel();
			this.DataContext = ViewModel;
		}

		private void ToggleSetting_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggleSwitch)
			{
				
			}
		}

		private void ApplySetting(string settingName, bool isEnabled)
		{
			// Implement the logic to apply each setting
			switch (settingName)
			{
				case "DisableJavaScriptToggle":
					// Apply JavaScript disabling logic
					break;
				case "DisableGeneralAutoFillToggle":
					// Apply autofill disabling logic
					break;
				case "DisableWebMessFillToggle":
					// Apply web messages disabling logic
					break;
				case "PasswordWebMessFillToggle":
					// Apply password autosave disabling logic
					break;
			}
		}

		private void Permission_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggleSwitch && toggleSwitch.DataContext is PermissionItem permissionItem)
			{
				
			}
		}

		private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is PermissionItem permissionItem)
			{

			}
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

		private void LoadPermissions()
		{
			// Load permissions from your storage or service
			// This is a placeholder implementation
			Permissions.Add(new PermissionItem { Url = "example.com", PermissionType = "Location", IsAllowed = true, Icon = "\uE1D2" });
			Permissions.Add(new PermissionItem { Url = "test.com", PermissionType = "Camera", IsAllowed = false, Icon = "\uE714" });
		}

		public void UpdatePermission(PermissionItem permissionItem)
		{
			// Update the permission in your storage or service
			// This is a placeholder implementation
			var index = Permissions.IndexOf(permissionItem);
			if (index != -1)
			{
				Permissions[index] = permissionItem;
			}
		}

		public void DeletePermission(PermissionItem permissionItem)
		{
			// Delete the permission from your storage or service
			// This is a placeholder implementation
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