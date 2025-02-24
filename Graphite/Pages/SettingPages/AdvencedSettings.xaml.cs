using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Graphite.UserSys;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Graphite.Pages.SettingPages
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class AdvencedSettings : Page
	{
		public AdvencedSettings()
		{
			this.InitializeComponent();
			InitializeControls();
			InitializeStartupToggle();

		}

		private void InitializeControls()
		{
			string username = UserManager.GetCurrentUsername();

			// Initialize Logger ComboBox
			Logger.ItemsSource = new List<string> { "Low", "Medium", "High" };
			LoadLoggerSetting(username);
			LoadMicaSetting(username);
		}

		private async void LoadLoggerSetting(string username)
		{
			var logLevel = await SettingsManager.GetSettingAsync<string>(username, "ExceptionLoggingLevel", "Low");
			Logger.SelectedItem = logLevel;
		}

		private async void LoadMicaSetting(string username)
		{
			var backdropStyle = await SettingsManager.GetSettingAsync<string>(username, "BackdropStyle", "Mica");
			BackDrop.SelectedItem = backdropStyle;
		}

		private async void Logger_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Logger.SelectedItem is string selectedLevel)
			{
				string username = UserManager.GetCurrentUsername();
				await SettingsManager.UpdateSettingAsync(username, "ExceptionLoggingLevel", selectedLevel);
			}
		}

		private async void BackDrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (BackDrop.SelectedItem is string selectedStyle)
			{
				string username = UserManager.GetCurrentUsername();
				await SettingsManager.UpdateSettingAsync(username, "BackdropStyle", selectedStyle);
			}
		}

		private async void LaunchOnStartupToggle_Click(object sender, RoutedEventArgs e)
		{
			await ToggleLaunchOnStartup(LaunchOnStartupToggle.IsChecked ?? false);
		}

		private async void InitializeStartupToggle()
		{
			StartupTask startup = await StartupTask.GetAsync("GraphiteStartUp");
			UpdateToggleState(startup.State);
		}

		private void UpdateToggleState(StartupTaskState state)
		{
			LaunchOnStartupToggle.IsEnabled = state != StartupTaskState.DisabledByPolicy;
			LaunchOnStartupToggle.IsChecked = state == StartupTaskState.Enabled;
		}

		private async Task ToggleLaunchOnStartup(bool enable)
		{
			StartupTask startup = await StartupTask.GetAsync("GraphiteStartUp");

			switch (startup.State)
			{
				case StartupTaskState.Enabled when !enable:
					startup.Disable();
					break;
				case StartupTaskState.Disabled when enable:
					StartupTaskState updatedState = await startup.RequestEnableAsync();
					UpdateToggleState(updatedState);
					break;
				case StartupTaskState.DisabledByUser when enable:
					await ShowContentDialogAsync("Unable to change state of startup task via the application", "Enable via Startup tab on Task Manager (Ctrl+Shift+Esc)");
					break;
				default:
					await ShowContentDialogAsync("Unable to change state of startup task");
					break;
			}
		}

		private async Task ShowContentDialogAsync(string title, string content = null)
		{
			ContentDialog dialog = new()
			{
				Title = title,
				Content = content,
				PrimaryButtonText = "OK",
				XamlRoot = this.Content.XamlRoot,
			};
			_ = await dialog.ShowAsync();
		}	
	}
}
