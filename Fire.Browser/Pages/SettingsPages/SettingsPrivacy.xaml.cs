using Fire.Browser.Core;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace FireBrowserWinUi3.Pages.SettingsPages;

public sealed partial class SettingsPrivacy : Page
{
	SettingsService SettingsService { get; set; }

	public SettingsPrivacy()
	{
		SettingsService = App.GetService<SettingsService>();
		this.InitializeComponent();
		Stack();
	}

	public void Stack()
	{
		DisableJavaScriptToggle.IsOn = SettingsService.CoreSettings.DisableJavaScript;
		DisablWebMessFillToggle.IsOn = SettingsService.CoreSettings.DisableWebMess;
		DisableGenaralAutoFillToggle.IsOn = SettingsService.CoreSettings.DisableGenAutoFill;
		PasswordWebMessFillToggle.IsOn = SettingsService.CoreSettings.DisablePassSave;
	}

	private async void ToggleSetting(string settingName, bool value)
	{
		switch (settingName)
		{
			case "DisableJavaScriptToggle":
				SettingsService.CoreSettings.DisableJavaScript = value;
				break;
			case "DisableGenaralAutoFillToggle":
				SettingsService.CoreSettings.DisableGenAutoFill = value;
				break;
			case "DisablWebMessFillToggle":
				SettingsService.CoreSettings.DisableWebMess = value;
				break;
			case "PasswordWebMessFillToggle":
				SettingsService.CoreSettings.DisablePassSave = value;
				break;
			// Add other cases for different settings.
			default:
				throw new ArgumentException("Invalid setting name");
		}

		// Save the modified settings back to the user's settings file
		await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
	}

	private void ToggleSetting_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			var autoSettingValue = toggleSwitch.IsOn;
			var settingName = toggleSwitch.Name;
			ToggleSetting(settingName, autoSettingValue);
		}
	}

	private void CamPermission_Toggled(object sender, RoutedEventArgs e)
	{

	}
}