using Riverside.Graphite.Core;
using Riverside.Graphite.Services;
using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Pages;
public sealed partial class InPrivate : Page
{
	private readonly Settings userSettings = new Settings(true).Self; // new UserFolderManager.TempLoadPrivate("Private");

	public InPrivate()
	{
		InitializeComponent();
		Init();
	}

	public void Init()
	{
		JavToggle.IsOn = userSettings.DisableJavaScript;
		WebToggle.IsOn = userSettings.DisableWebMess;
	}

	private void ToggleSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			userSettings.DisableJavaScript = autoSettingValue;

			AppService.AppSettings = userSettings;
			//UserFolderManager.TempSaveSettings("Private", userSettings);
		}
	}

	private void ToggleSwitch_Toggled_1(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			bool autoSettingValue = toggleSwitch.IsOn;

			userSettings.DisableWebMess = autoSettingValue;

			AppService.AppSettings = userSettings;
			//UserFolderManager.TempSaveSettings("Private", userSettings);
		}
	}
}