using Fire.Browser.Core;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.SpeechSynthesis;

namespace FireBrowserWinUi3.Pages.SettingsPages;

public sealed partial class SettingsAccess : Page
{
	private bool _isMode;
	private SettingsService SettingsService { get; }

	public SettingsAccess()
	{
		InitializeComponent();
		SettingsService = App.GetService<SettingsService>();
		LoadUserDataAndSettings();
		InitializeStartupToggle();
		ShowSupportedLanguagesAndGenders();
	}

	private void ShowSupportedLanguagesAndGenders()
	{
		IReadOnlyList<VoiceInformation> allVoices = SpeechSynthesizer.AllVoices;

		PopulateComboBox(Langue, allVoices.Select(v => v.Language).Distinct(), SettingsService.CoreSettings.Lang);
		PopulateComboBox(Gender, allVoices.Select(v => v.Gender.ToString()).Distinct(), SettingsService.CoreSettings.Gender);
	}

	private void PopulateComboBox(ComboBox comboBox, IEnumerable<string> items, string storedValue)
	{
		comboBox.ItemsSource = items.ToList();
		comboBox.SelectedItem = items.Contains(storedValue) ? storedValue : comboBox.Items.FirstOrDefault();
	}

	private void LoadUserDataAndSettings()
	{
		Langue.SelectedValue = SettingsService.CoreSettings.Lang;
		Logger.SelectedValue = SettingsService.CoreSettings.ExceptionLog;
		LiteMode.IsOn = SettingsService.CoreSettings.LightMode;
	}

	private async void LiteMode_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			_isMode = toggleSwitch.IsOn;
			SettingsService.CoreSettings.LightMode = _isMode;
			await SaveSettingsAsync();
		}
	}

	private async void Langue_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is string selectedLanguage)
		{
			SettingsService.CoreSettings.Lang = selectedLanguage;
			await SaveSettingsAsync($"Language setting updated to: {selectedLanguage}");
		}
	}

	private async void InitializeStartupToggle()
	{
		StartupTask startup = await StartupTask.GetAsync("FireBrowserWinUiStartUp");
		UpdateToggleState(startup.State);
	}

	private void UpdateToggleState(StartupTaskState state)
	{
		LaunchOnStartupToggle.IsEnabled = state != StartupTaskState.DisabledByPolicy;
		LaunchOnStartupToggle.IsChecked = state == StartupTaskState.Enabled;
	}

	private async Task ToggleLaunchOnStartup(bool enable)
	{
		StartupTask startup = await StartupTask.GetAsync("FireBrowserWinUiStartUp");

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
			PrimaryButtonText = "OK"
		};
		_ = await dialog.ShowAsync();
	}

	private async void LaunchOnStartupToggle_Click(object sender, RoutedEventArgs e)
	{
		await ToggleLaunchOnStartup(LaunchOnStartupToggle.IsChecked ?? false);
	}

	private async void Logger_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is string selection)
		{
			SettingsService.CoreSettings.ExceptionLog = selection switch
			{
				"Low" => "Low",
				"High" => "High",
				_ => throw new ArgumentException("Invalid selection")
			};
			await SaveSettingsAsync();
		}
	}

	private void WelcomeMesg_Toggled(object sender, RoutedEventArgs e)
	{
		// Implement welcome message toggle logic here
	}

	private async void Gender_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is string selectedGender)
		{
			SettingsService.CoreSettings.Gender = selectedGender;
			await SaveSettingsAsync($"Gender setting updated to: {selectedGender}");
		}
	}

	private async Task SaveSettingsAsync(string debugMessage = null)
	{
		try
		{
			await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
			if (debugMessage != null)
			{
				Debug.WriteLine(debugMessage);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error updating setting: {ex.Message}");
		}
	}
}