using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System;
using Graphite.UserSys;
using Microsoft.UI;
using Graphite.UserSys.Windows;
using Graphite.Helpers;

namespace Graphite.Migration
{
	public sealed partial class MigrationWindow : Window
	{
		private AppWindow m_AppWindow;

		public MigrationWindow()
		{
			this.InitializeComponent();

			m_AppWindow = GetAppWindowForCurrentWindow();

			if (AppWindowTitleBar.IsCustomizationSupported())
			{
				var titleBar = m_AppWindow.TitleBar;
				titleBar.ButtonBackgroundColor = Colors.Transparent;
				titleBar.ButtonForegroundColor = Colors.Transparent;
				titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
				titleBar.ButtonInactiveForegroundColor = Colors.Transparent;
				titleBar.ExtendsContentIntoTitleBar = true;
			}
			else
			{
				AppTitleBar.Visibility = Visibility.Collapsed;
			}

		}

		private AppWindow GetAppWindowForCurrentWindow()
		{
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			return AppWindow.GetFromWindowId(wndId);
		}

		private void TransferButton_Click(object sender, RoutedEventArgs e)
		{
			// Show transfer options and hide main options
			MainOptionsPanel.Visibility = Visibility.Collapsed;
			TransferOptionsPanel.Visibility = Visibility.Visible;
		}

		private void ImportButton_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void ReceiverButton_Click(object sender, RoutedEventArgs e)
		{
			var receiverWindow = new ReceiverWindow();
			receiverWindow.Activate();
			this.Close();

		}

		private void SenderButton_Click(object sender, RoutedEventArgs e)
		{
			var senderWindow = new SenderWindow();
			senderWindow.Activate();
		}

		private void ImportFromFireBrowserButton_Click(object sender, RoutedEventArgs e)
		{
			// Start the MigrationProgress window in UserSys
			MigrationProgress migrationProgressWindow = new MigrationProgress();
			Windowing.Center(migrationProgressWindow);	
			migrationProgressWindow.Activate();
		}
	}
}

