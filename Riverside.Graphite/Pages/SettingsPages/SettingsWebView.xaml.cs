using Riverside.Graphite.Core;
using Riverside.Graphite.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;

namespace Riverside.Graphite.Pages.SettingsPages;
public sealed partial class SettingsWebView : Page
{
	private SettingsService SettingsService { get; set; }

	private readonly WebView2 Web = new();
	public SettingsWebView()
	{
		SettingsService = App.GetService<SettingsService>();
		InitializeComponent();
		loadinit();
	}

	public void loadinit()
	{
		Agent.Text = SettingsService.CoreSettings.Useragent;
		StatusTog.IsOn = SettingsService.CoreSettings.StatusBar;
		BrowserKeys.IsOn = SettingsService.CoreSettings.BrowserKeys;
		BrowserScripts.IsOn = SettingsService.CoreSettings.BrowserScripts;
		ResourceSaver.IsOn = SettingsService.CoreSettings.ResourceSave;
		PipModeTg.IsOn = SettingsService.CoreSettings.PipMode;
		antitracklevel();
	}

	public void antitracklevel()
	{
		// Assuming SettingsService.CoreSettings.TrackPrevention is a string that may be null or contain a valid number (0, 1, 2, 3)
		int trackPreventionSetting = SettingsService.CoreSettings.TrackPrevention;

		// Map the numeric value to the corresponding text value
		string selectedText = trackPreventionSetting switch
		{
			0 => "None",
			1 => "Basic",
			2 => "Balanced",
			3 => "Strict",
			_ => "Balanced",// You may want to handle unexpected values here
		};

		// Assuming PreventionLevel.ItemsSource contains the text values ("None", "Basic", "Balanced", "Strict")
		PreventionLevel.SelectedItem = selectedText;
	}
	private async void ClearCookies_Click(object sender, RoutedEventArgs e)
	{
		await Web.EnsureCoreWebView2Async();
		Web.CoreWebView2.CookieManager.DeleteAllCookies();
	}

	private void ClearCache_Click(object sender, RoutedEventArgs e)
	{
		ClearAutofillData();
	}

	private async void ClearAutofillData()
	{
		CoreWebView2Profile profile;
		if (Web.CoreWebView2 != null)
		{
			profile = Web.CoreWebView2.Profile;
			// Get the current time, the time in which the browsing data will be cleared
			// until.
			System.DateTime endTime = DateTime.Now;
			System.DateTime startTime = DateTime.Now.AddHours(-1);
			// Offset the current time by one hour to clear the browsing data from the
			// last hour.
			CoreWebView2BrowsingDataKinds dataKinds =
									 CoreWebView2BrowsingDataKinds.DiskCache |
									  CoreWebView2BrowsingDataKinds.CacheStorage;
			await profile.ClearBrowsingDataAsync(dataKinds, startTime, endTime);
		}
	}

	private async void Agent_TextChanged(object sender, TextChangedEventArgs e)
	{
		string autoSettingValue = Agent.Text.ToString();

		SettingsService.CoreSettings.Useragent = autoSettingValue;

		await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
	}

	private async void StatusTog_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			SettingsService.CoreSettings.StatusBar = autoSettingValue;

			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void BrowserKeys_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			SettingsService.CoreSettings.BrowserKeys = autoSettingValue;

			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void BrowserScripts_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			SettingsService.CoreSettings.BrowserScripts = autoSettingValue;

			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void PreventionLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			string selection = e.AddedItems[0].ToString();
			int antitrack;

			switch (selection)
			{
				case "None":
					antitrack = 0;
					Info.Text = "No Privacy Anti Tracking No Effect On Websites";
					break;
				case "Basic":
					antitrack = 1;
					Info.Text = "Basic Privacy Anti Tracking Small Effect On Websites";
					break;
				case "Balanced":
					antitrack = 2;
					Info.Text = "Balanced Privacy Anti Tracking High Level Works With Most Sites";
					break;
				case "Strict":
					antitrack = 3;
					Info.Text = "Strict Privacy Anti Tracking Can Break Some Websites";
					break;

				default:
					// Handle the case when selection doesn't match any of the predefined options.
					antitrack = 2;
					break;
			}


			SettingsService.CoreSettings.TrackPrevention = antitrack;
			// Save the modified settings back to the user's settings file
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
		catch (Exception ex)
		{
			Console.WriteLine("An error occurred: " + ex.Message);
		}
	}

	private async void ResourceSaver_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			SettingsService.CoreSettings.ResourceSave = autoSettingValue;

			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void AdBlocker_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			SettingsService.CoreSettings.AdblockBtn = autoSettingValue;

			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}


	private async void PipModeTg_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			SettingsService.CoreSettings.PipMode = autoSettingValue;

			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}
}