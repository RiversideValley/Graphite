using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Riverside.Graphite.Core;
using Riverside.Graphite.Services;

namespace Riverside.Graphite.Pages.SettingsPages;
public sealed partial class SettingsDesign : Page
{
	private SettingsService SettingsService { get; set; }

	public SettingsDesign()
	{
		SettingsService = App.GetService<SettingsService>();

		InitializeComponent();
		Init();
		Check();
	}

	public void Init()
	{
		AutoTog.IsOn = SettingsService.CoreSettings.Auto;
	}

	public void Check()
	{
		Type.SelectedItem = SettingsService.CoreSettings.Background switch
		{
			0 => "Default",
			1 => "Featured",
			2 => "Custom",
			_ => Type.SelectedItem
		};
	}

	private async void AutoTog_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			// Assuming 'url' and 'selection' have been defined earlier
			bool autoSettingValue = toggleSwitch.IsOn;

			// Set the 'Auto' setting
			SettingsService.CoreSettings.Auto = autoSettingValue;

			// Save the modified settings back to the user's settings file
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		string selection = e.AddedItems[0].ToString();
		if (selection == "Default")
		{
			Color.IsEnabled = false;
			SettingsService.CoreSettings.Background = 0;
		}
		if (selection == "Featured")
		{
			Color.IsEnabled = false;
			SettingsService.CoreSettings.Background = 1;
		}
		if (selection == "Custom")
		{
			Color.IsEnabled = true;
			SettingsService.CoreSettings.Background = 2;
		}

		await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
	}

	private async void ColorTBPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		if (AuthService.CurrentUser != null)
		{
			// Update the "ColorBackground" setting for the current user
			SettingsService.CoreSettings.ColorTool = XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), ColorTBPicker.Color).ToString();

			// Save the modified settings back to the user's settings file
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void ColorTVPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		if (AuthService.CurrentUser != null)
		{
			// Update the "ColorBackground" setting for the current user
			SettingsService.CoreSettings.ColorTV = XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), ColorTVPicker.Color).ToString();

			// Save the modified settings back to the user's settings file
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void ColorNtpPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		if (AuthService.CurrentUser != null)
		{
			// Update the "ColorBackground" setting for the current user
			SettingsService.CoreSettings.NtpTextColor = XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), ColorNtpPicker.Color).ToString();

			// Save the modified settings back to the user's settings file
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}

	private async void ColorBackGroundPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		if (AuthService.CurrentUser != null)
		{
			// Update the "ColorBackground" setting for the current user
			SettingsService.CoreSettings.ColorBackground = XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), ColorBackGroundPicker.Color).ToString();

			// Save the modified settings back to the user's settings file
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}
}