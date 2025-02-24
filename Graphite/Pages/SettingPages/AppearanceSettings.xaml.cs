using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Graphite.UserSys;
using System;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class AppearanceSettings : Page
	{
		private string currentUsername;

		public AppearanceSettings()
		{
			this.InitializeComponent();
			currentUsername = UserManager.GetCurrentUsername();
			InitializeSettings();
		}

		private async void InitializeSettings()
		{
			// Initialize color pickers
			ColorTBPicker.Color = HexToColor(await SettingsManager.GetSettingAsync<string>(currentUsername, "ToolbarColor", "#00FFFFFF"));
			ColorTVPicker.Color = HexToColor(await SettingsManager.GetSettingAsync<string>(currentUsername, "TabViewColor", "#00FFFFFF"));
			ColorNtpPicker.Color = HexToColor(await SettingsManager.GetSettingAsync<string>(currentUsername, "NewTabPageTextColor", "#00FFFFFF"));
			ColorBackGroundPicker.Color = HexToColor(await SettingsManager.GetSettingAsync<string>(currentUsername, "BackgroundColor", "#00FFFFFF"));

			// Initialize background type
			Type.SelectedIndex = await SettingsManager.GetSettingAsync<int>(currentUsername, "BackgroundType", 0);

			// Initialize toggles
			InitializeToggle(AutoTog, "EnableAutoFill");
			InitializeToggle(BackSettings, "ShowBackButton");
			InitializeToggle(ForwardSettings, "ShowForwardButton");
			InitializeToggle(ReloadSettings, "ShowRefreshButton");
			InitializeToggle(HomeSettings, "ShowHomeButton");
			InitializeToggle(Dwbl, "ShowDownloadsButton");
			InitializeToggle(Frbl, "ShowFavoritesList");
			InitializeToggle(FlAd, "ShowFavoritesButton");
			InitializeToggle(Hsbl, "ShowHistoryButton");
			InitializeToggle(Qrbl, "ShowQRCodeButton");
			InitializeToggle(Tlbl, "ShowToolbarIcons");
			InitializeToggle(Drbl, "ShowDarkModeIcon");
			InitializeToggle(Trbl, "ShowTranslateButton");
			InitializeToggle(Read, "ShowReadButton");
			InitializeToggle(Adbl, "ShowAdBlockButton");
			InitializeToggle(SearchHome, "IsSearchBarVisible");
			InitializeToggle(HistoryHome, "IsHistoryPanelVisible");
			InitializeToggle(FavoritesHome, "IsFavoritesPanelVisible");
			InitializeToggle(TrendingHome, "ShowTrendingContent");
			InitializeToggle(LogoHome, "ShowBrowserLogo");
			InitializeToggle(SelectbarHome, "ShowNewTabSelectorBar");

			// Wire up events
			ColorTBPicker.ColorChanged += ColorPicker_ColorChanged;
			ColorTVPicker.ColorChanged += ColorPicker_ColorChanged;
			ColorNtpPicker.ColorChanged += ColorPicker_ColorChanged;
			ColorBackGroundPicker.ColorChanged += ColorPicker_ColorChanged;
			Type.SelectionChanged += Type_SelectionChanged;
		}

		private async void InitializeToggle(ToggleSwitch toggle, string settingName)
		{
			toggle.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, settingName, true);
			toggle.Toggled += Toggle_Toggled;
		}

		private Windows.UI.Color HexToColor(string hex)
		{
			hex = hex.Replace("#", string.Empty);
			byte a, r, g, b;

			if (hex.Length == 6)
			{
				a = 255;
				r = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
				g = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
				b = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
			}
			else if (hex.Length == 8)
			{
				a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
				r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
				g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
				b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
			}
			else
			{
				throw new ArgumentException("Invalid hex color string", nameof(hex));
			}

			return new Windows.UI.Color() { A = a, R = r, G = g, B = b };
		}

		private string ColorToHex(Color color)
		{
			return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
		}

		private async void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			string settingName = sender.Name switch
			{
				"ColorTBPicker" => "ToolbarColor",
				"ColorTVPicker" => "TabViewColor",
				"ColorNtpPicker" => "NewTabPageTextColor",
				"ColorBackGroundPicker" => "BackgroundColor",
				_ => throw new ArgumentException("Unknown ColorPicker")
			};

			await SettingsManager.UpdateSettingAsync(currentUsername, settingName, ColorToHex(args.NewColor));
		}

		private async void Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "BackgroundType", Type.SelectedIndex);
		}

		private async void Toggle_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggle)
			{
				string settingName = toggle.Name switch
				{
					"AutoTog" => "EnableAutoFill",
					"BackSettings" => "ShowBackButton",
					"ForwardSettings" => "ShowForwardButton",
					"ReloadSettings" => "ShowRefreshButton",
					"HomeSettings" => "ShowHomeButton",
					"Dwbl" => "ShowDownloadsButton",
					"Frbl" => "ShowFavoritesList",
					"FlAd" => "ShowFavoritesButton",
					"Hsbl" => "ShowHistoryButton",
					"Qrbl" => "ShowQRCodeButton",
					"Tlbl" => "ShowToolbarIcons",
					"Drbl" => "ShowDarkModeIcon",
					"Trbl" => "ShowTranslateButton",
					"Read" => "ShowReadButton",
					"Adbl" => "ShowAdBlockButton",
					"SearchHome" => "IsSearchBarVisible",
					"HistoryHome" => "IsHistoryPanelVisible",
					"FavoritesHome" => "IsFavoritesPanelVisible",
					"TrendingHome" => "ShowTrendingContent",
					"LogoHome" => "ShowBrowserLogo",
					"SelectbarHome" => "ShowNewTabSelectorBar",
					_ => throw new ArgumentException("Unknown ToggleSwitch")
				};

				await SettingsManager.UpdateSettingAsync(currentUsername, settingName, toggle.IsOn);
			}
		}
	}
}