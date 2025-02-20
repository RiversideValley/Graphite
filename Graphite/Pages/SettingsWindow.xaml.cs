using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Graphite.UserSys;
using WinRT.Interop;
using Graphite.Pages.SettingsPages;

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
			ContentFrame.Navigate(typeof(TabSettings), USR);
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
					"GeneralPage" => typeof(TabSettings),
					"PrivacyPage" => typeof(TabSettings),
					"AppearancePage" => typeof(TabSettings),
					"DownloadsPage" => typeof(TabSettings),
					"HomepagePage" => typeof(TabSettings),
					"ShortcutsPage" => typeof(TabSettings),
					"ExtensionsPage" => typeof(TabSettings),
					"TabsSettings" => typeof(TabSettings),
					"AdvancedPage" => typeof(TabSettings),
					"AboutPage" => typeof(TabSettings),
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

