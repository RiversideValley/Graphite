using Graphite.Controls;
using Graphite.Helpers;
using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Graphite.Pages.SettingsPages;
public sealed partial class TabSettings : Page
{
	public TabSettings()
	{
		this.InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		LoadUserSettings();
		LoadTabStates();
	}

	private void LoadTabStates()
	{
		string currentUsername = UserManager.GetCurrentUsername();
		var tabStates = TabManager.GetStoredTabStates(currentUsername);

		if (tabStates == null || tabStates.Count == 0)
		{
			TabStateListView.Visibility = Visibility.Collapsed;
			NoItemsMessage.Visibility = Visibility.Visible;
		}
		else
		{
			TabStateListView.ItemsSource = tabStates;
			TabStateListView.Visibility = Visibility.Visible;
			NoItemsMessage.Visibility = Visibility.Collapsed;
		}
	}

	private async void ClearTabHistoryButton_Click(object sender, RoutedEventArgs e)
	{
		ContentDialog dialog = new ContentDialog
		{
			Title = "Clear Tab History",
			Content = "Are you sure you want to clear all tab history? This action cannot be undone.",
			PrimaryButtonText = "Clear",
			CloseButtonText = "Cancel"
		};

		dialog.XamlRoot = this.XamlRoot;

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary)
		{
			// Implement the logic to clear tab history
			string currentUsername = UserManager.GetCurrentUsername();
			var localSettings = ApplicationData.Current.LocalSettings;
			string cacheKey = $"{currentUsername}_{TabManager.TabStateKey}";
			localSettings.Values.Remove(cacheKey);

			// Refresh the ListView
			LoadTabStates();
		}
	}

	private async void EditTabState_Click(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;
		var tabState = button.DataContext as TabState;

		if (tabState != null)
		{
			var nameBox = new TextBox { Text = tabState.Header, PlaceholderText = "Enter tab title", Header = "Tab title" };
			var urlBox = new TextBox { Text = tabState.Url, PlaceholderText = "Enter URL", Header = "URL" };

			var panel = new StackPanel();
			panel.Children.Add(nameBox);
			panel.Children.Add(urlBox);

			ContentDialog dialog = new ContentDialog
			{
				Title = "Edit Tab State",
				Content = panel,
				PrimaryButtonText = "Save",
				CloseButtonText = "Cancel"
			};

			dialog.XamlRoot = this.XamlRoot;

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				var newName = nameBox.Text;
				var newUrl = urlBox.Text;

				if (!string.IsNullOrWhiteSpace(newName) && !string.IsNullOrWhiteSpace(newUrl))
				{
					// Update the tabState object
					tabState.Header = newName;
					tabState.Url = newUrl;

					// Update the state in the state store
					string currentUsername = UserManager.GetCurrentUsername();
					var tabStates = TabManager.GetStoredTabStates(currentUsername);
					var index = tabStates.FindIndex(t => t.Header == tabState.Header); // Assuming TabState has an Id property
					if (index != -1)
					{
						tabStates[index] = tabState;
						SaveTabStates(currentUsername, tabStates);
					}

					// Refresh the ListView
					UpdateStoredTabStates();
				}
				else
				{
					// Show an error message if either field is empty
					ContentDialog errorDialog = new ContentDialog
					{
						Title = "Error",
						Content = "Both title and URL must be provided.",
						CloseButtonText = "OK"
					};
					errorDialog.XamlRoot = this.XamlRoot;
					await errorDialog.ShowAsync();
				}
			}
		}
	}

	// Add this method to TabManager class if it doesn't exist
	public static void SaveTabStates(string username, List<TabState> tabStates)
	{
		var json = JsonSerializer.Serialize(tabStates);
		var localSettings = ApplicationData.Current.LocalSettings;
		string cacheKey = $"{username}_{TabManager.TabStateKey}";
		localSettings.Values[cacheKey] = json;
	}

	private async void DeleteTabState_Click(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;
		var tabState = button.DataContext as TabState;

		if (tabState != null)
		{
			ContentDialog dialog = new ContentDialog
			{
				Title = "Delete Tab State",
				Content = $"Are you sure you want to delete the tab state for {tabState.PageTitle} ({tabState.Url})?",
				PrimaryButtonText = "Delete",
				CloseButtonText = "Cancel"
			};

			dialog.XamlRoot = this.XamlRoot;

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				var tabStates = TabStateListView.ItemsSource as List<TabState>;
				tabStates.Remove(tabState);
				UpdateStoredTabStates();
			}
		}
	}

	private void UpdateStoredTabStates()
	{
		string currentUsername = UserManager.GetCurrentUsername();
		var tabStates = TabStateListView.ItemsSource as List<TabState>;
		var json = JsonSerializer.Serialize(tabStates);
		var localSettings = ApplicationData.Current.LocalSettings;
		string cacheKey = $"{currentUsername}_{TabManager.TabStateKey}";
		localSettings.Values[cacheKey] = json;

		// Refresh the ListView
		LoadTabStates();
	}

	private async void LoadUserSettings()
	{
		string username = UserManager.GetCurrentUsername();
		if (!string.IsNullOrEmpty(username))
		{
			await LoadSettingsAsync();
		}
		else
		{
			await ShowMessageAsync("Error: User data is not available.");
		}
	}

	private async Task LoadSettingsAsync()
	{
		string username = UserManager.GetCurrentUsername();
		AutoRestoreToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(username, "GenaralTabAutoRestore", true);
		MaxTabsBox.Value = await SettingsManager.GetSettingAsync<int>(username, "GenaralTabMaxNumber", 50);
		SleepTimerBox.Value = await SettingsManager.GetSettingAsync<int>(username, "TabSleepTime", 30);
		PreloadTabsToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(username, "TabPreloading", true);
		TabGroupingToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(username, "TabGroupingEnabled", false);
		PreloadedTabsBox.Value = await SettingsManager.GetSettingAsync<int>(username, "PreloadedTabsCount", 1);
	}

	private async void AutoRestoreToggle_Toggled(object sender, RoutedEventArgs e)
	{
		string username = UserManager.GetCurrentUsername();
		await SettingsManager.UpdateSettingAsync(username, "GenaralTabAutoRestore", AutoRestoreToggle.IsOn);
	}

	private async void MaxTabsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		string username = UserManager.GetCurrentUsername();
		if (string.IsNullOrEmpty(username))
		{
			await ShowMessageAsync("Error: User data is not available. Please try reloading the page.");
			return;
		}

		if (!double.IsNaN(sender.Value))
		{
			int maxTabs = (int)Math.Round(sender.Value);
			try
			{
				await SettingsManager.UpdateSettingAsync(username, "GenaralTabMaxNumber", maxTabs.ToString());
			}
			catch (Exception ex)
			{
				await ShowMessageAsync($"Failed to update setting: {ex.Message}");
			}
		}
	}

	private async void SleepTimerBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		string username = UserManager.GetCurrentUsername();
		if (!double.IsNaN(sender.Value))
		{
			int sleepTime = (int)Math.Round(sender.Value);
			await SettingsManager.UpdateSettingAsync(username, "TabSleepTime", sleepTime.ToString());
		}
	}

	private async void PreloadTabsToggle_Toggled(object sender, RoutedEventArgs e)
	{
		string username = UserManager.GetCurrentUsername();
		await SettingsManager.UpdateSettingAsync(username, "TabPreloading", PreloadTabsToggle.IsOn);
	}

	private async void TabGroupingToggle_Toggled(object sender, RoutedEventArgs e)
	{
		string username = UserManager.GetCurrentUsername();
		await SettingsManager.UpdateSettingAsync(username, "TabGroupingEnabled", TabGroupingToggle.IsOn);
	}

	private async void PreloadedTabsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		string username = UserManager.GetCurrentUsername();
		if (!double.IsNaN(sender.Value))
		{
			int preloadedTabs = (int)Math.Round(sender.Value);
			await SettingsManager.UpdateSettingAsync(username, "PreloadedTabsCount", preloadedTabs.ToString());
		}
	}

	private async Task ShowMessageAsync(string message)
	{
		ContentDialog dialog = new ContentDialog();
		dialog.XamlRoot = this.Content.XamlRoot;
		dialog.Title = "Settings Error";
		dialog.Content = message;
		dialog.CloseButtonText = "Close";
		await dialog.ShowAsync();
	}
}