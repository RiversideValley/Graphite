using Riverside.Graphite.Core;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Pages.SettingsPages
{
	public sealed partial class SettingsNewTab : Page
	{
		private SettingsService SettingsService { get; }
		private static readonly Dictionary<string, string> SearchEngines = new()
		{
			{ "Ask", "https://www.ask.com/web?q=" },
			{ "Baidu", "https://www.baidu.com/s?ie=utf-8&f=8&rsv_bp=1&rsv_idx=1&tn=baidu&wd=" },
			{ "Bing", "https://www.bing.com?q=" },
			{ "DuckDuckGo", "https://www.duckduckgo.com?q=" },
			{ "Ecosia", "https://www.ecosia.org/search?q=" },
			{ "Google", "https://www.google.com/search?q=" },
			{ "Startpage", "https://www.startpage.com/search?q=" },
			{ "Qwant", "https://www.qwant.com/?q=" },
			{ "Qwant Lite", "https://lite.qwant.com/?q=" },
			{ "Yahoo!", "https://search.yahoo.com/search?p=" },
			{ "Presearch", "https://presearch.com/search?q=" },
			{ "Swisscows", "https://swisscows.com/web?query=" },
			{ "Dogpile", "https://www.dogpile.com/serp?q=" },
			{ "Webcrawler", "https://www.webcrawler.com/serp?q=" },
			{ "You", "https://you.com/search?q=" },
			{ "Excite", "https://results.excite.com/serp?q=" },
			{ "Lycos", "https://search20.lycos.com/web/?q=" },
			{ "Metacrawler", "https://www.metacrawler.com/serp?q=" },
			{ "Mojeek", "https://www.mojeek.com/search?q=" },
			{ "BraveSearch", "https://search.brave.com/search?q=" },
		};

		public SettingsNewTab()
		{
			SettingsService = App.GetService<SettingsService>();
			InitializeComponent();
			LoadSettings();
		}

		private void LoadSettings()
		{
			SearchengineSelection.SelectedItem = SettingsService.CoreSettings.EngineFriendlyName;

			SetToggleSwitches();
		}

		private void SetToggleSwitches()
		{
			OpenNew.IsOn = SettingsService.CoreSettings.OpenTabHandel;
			Trbl.IsOn = SettingsService.CoreSettings.Translate;
			Adbl.IsOn = SettingsService.CoreSettings.AdblockBtn;
			Drbl.IsOn = SettingsService.CoreSettings.DarkIcon;
			Read.IsOn = SettingsService.CoreSettings.ReadButton;
			Dwbl.IsOn = SettingsService.CoreSettings.Downloads;
			Frbl.IsOn = SettingsService.CoreSettings.FavoritesL;
			FlAd.IsOn = SettingsService.CoreSettings.Favorites;
			Hsbl.IsOn = SettingsService.CoreSettings.Historybtn;
			Qrbl.IsOn = SettingsService.CoreSettings.QrCode;
			Tlbl.IsOn = SettingsService.CoreSettings.ToolIcon;
			TrendingHome.IsOn = SettingsService.CoreSettings.IsTrendingVisible;
			FavoritesHome.IsOn = SettingsService.CoreSettings.IsFavoritesVisible;
			SearchHome.IsOn = SettingsService.CoreSettings.IsSearchVisible;
			HistoryHome.IsOn = SettingsService.CoreSettings.IsHistoryVisible;
			BackSettings.IsOn = SettingsService.CoreSettings.BackButton;
			ForwardSettings.IsOn = SettingsService.CoreSettings.ForwardButton;
			ReloadSettings.IsOn = SettingsService.CoreSettings.RefreshButton;
			HomeSettings.IsOn = SettingsService.CoreSettings.HomeButton;
			Confirm.IsOn = SettingsService.CoreSettings.ConfirmCloseDlg;
		}

		private async void SearchengineSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selection)
			{
				if (SearchEngines.TryGetValue(selection, out string url))
				{
					SettingsService.CoreSettings.EngineFriendlyName = selection;
					SettingsService.CoreSettings.SearchUrl = url;
					await SaveSettingsAsync();
				}
			}
		}

		private async void ToggleSetting_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggleSwitch)
			{
				string settingName = toggleSwitch.Tag as string;
				await ToggleSettingAsync(settingName, toggleSwitch.IsOn);
			}
		}

		private async Task ToggleSettingAsync(string settingName, bool value)
		{
			System.Reflection.PropertyInfo property = SettingsService.CoreSettings.GetType().GetProperty(settingName);
			if (property != null)
			{
				property.SetValue(SettingsService.CoreSettings, value);
				await SaveSettingsAsync();
			}
		}

		private void UpdateAppSetting(ToggleSwitch toggleSwitch, Action<bool> setter)
		{
			setter(toggleSwitch.IsOn);
		}

		private void TrendingHome_Toggled(object sender, RoutedEventArgs e)
		{
			UpdateAppSetting((ToggleSwitch)sender, value => AppService.AppSettings.IsTrendingVisible = value);
		}

		private void FavoritesHome_Toggled(object sender, RoutedEventArgs e)
		{
			UpdateAppSetting((ToggleSwitch)sender, value => AppService.AppSettings.IsFavoritesVisible = value);
		}

		private void HistoryHome_Toggled(object sender, RoutedEventArgs e)
		{
			UpdateAppSetting((ToggleSwitch)sender, value => AppService.AppSettings.IsHistoryVisible = value);
		}

		private void SearchHome_Toggled(object sender, RoutedEventArgs e)
		{
			UpdateAppSetting((ToggleSwitch)sender, value => AppService.AppSettings.IsSearchVisible = value);
		}

		private Task SaveSettingsAsync()
		{
			return SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
		}
	}
}