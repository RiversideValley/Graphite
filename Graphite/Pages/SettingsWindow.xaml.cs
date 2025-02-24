using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Graphite.UserSys;
using WinRT.Interop;
using Graphite.Pages.SettingsPages;
using Graphite.Pages.SettingPages;

namespace Graphite.Pages
{
	public sealed partial class SettingsWindow : Window
	{
		private AppWindow appWindow;
		private User USR;

		public SettingsWindow(User user)
		{
			this.InitializeComponent();
			USR = user;
			TitleTop();
			SettingsNav.ItemInvoked += SettingsNav_ItemInvoked;

			// Navigate to default page
			ContentFrame.Navigate(typeof(GeneralSettings), USR);
		}

		public void TitleTop()
		{
			nint hWnd = WindowNative.GetWindowHandle(this);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			appWindow = AppWindow.GetFromWindowId(windowId);
			appWindow.SetIcon("Logo.ico");
			appWindow.Title = $"Browser Settings - {USR.Username}";

			if (!AppWindowTitleBar.IsCustomizationSupported())
			{
				throw new Exception("Unsupported OS version.");
			}

			AppWindowTitleBar titleBar = appWindow.TitleBar;
			titleBar.ExtendsContentIntoTitleBar = true;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = titleBar.ButtonBackgroundColor =
				titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor =
				titleBar.ButtonHoverBackgroundColor = btnColor;
		}

		private void SettingsNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (args.InvokedItemContainer is NavigationViewItem item)
			{
				Type pageType = item.Tag.ToString() switch
				{
					"GeneralPage" => typeof(GeneralSettings),
					"PrivacyPage" => typeof(PrivacySettings),
					"WebViewPage" => typeof(WebViewSettings),
					"AppearancePage" => typeof(AppearanceSettings),
					"DownloadsPage" => typeof(DownloadsSettings),
					"ShortcutsPage" => typeof(ShortcutSettings),
					"ExtensionsPage" => typeof(ExtensionsSettings),
					"TabsSettings" => typeof(TabSettings),
					"Accessibility" => typeof(AccessibilitySettings),
					"AdvancedPage" => typeof(AdvencedSettings),
					"AboutPage" => typeof(AboutSettings),
					_ => null
				};

				if (pageType != null)
				{
					ContentFrame.Navigate(pageType, USR);
				}
			}
		}
	}
}

