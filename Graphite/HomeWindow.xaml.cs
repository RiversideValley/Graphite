using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Graphite.Controls;
using Graphite.Helpers;
using Graphite.Pages;
using Graphite.UserSys;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using WinRT.Interop;

namespace Graphite
{
	public sealed partial class HomeWindow : Window
	{
		private User _currentUser;
		private AppWindow appWindow;
		private readonly int maxTabItems = 30;
		private TabManager _tabManager;

		public HomeWindow(User user)
		{
			this.InitializeComponent();
			_currentUser = user;
			this.Title = $"Graphite Home Page - {_currentUser.Username}";
			if (QRCodeTypeComboBox.SelectedItem == null)
			{
				QRCodeTypeComboBox.SelectedIndex = 0;
			}
			UserName.Text = _currentUser.Username;
			_tabManager = new TabManager(Tabs);
			StartupTabCheckAsync();

			TitleTop();
			this.Closed += HomeWindow_Closed;
		}

		public async Task StartupTabCheckAsync()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			string cacheKey = $"{_currentUser.Username}_{TabManager.TabStateKey}";

			if (localSettings.Values.ContainsKey(cacheKey))
			{
				// Restore cache exists, attempt to restore tabs
				await _tabManager.RestoreTabsAsync(_currentUser.Username);
			}

			// Check if there are any tabs after restoration attempt
			if (Tabs.TabItems.Count == 0)
			{
				// No tabs, create a default tab
				_tabManager.CreateNewTab(typeof(NewTab));
			}

			await _tabManager.StartPreloadingTabs();
		}

		private void HomeWindow_Closed(object sender, WindowEventArgs args)
		{
			_tabManager.SaveTabStateAsync(_currentUser.Username.ToString());
		}

		public void TitleTop()
		{
			nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			appWindow = AppWindow.GetFromWindowId(windowId);
			appWindow.SetIcon("Logo.ico");

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

		private void Tabs_Loaded(object sender, RoutedEventArgs e)
		{
			Apptitlebar.SizeChanged += Apptitlebar_SizeChanged;
			Apptitlebar_LayoutUpdated(sender, e);
		}

		private void Apptitlebar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			try
			{
				double scaleAdjustment = GetScaleAdjustment();
				Apptitlebar.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
				Windows.Foundation.Point customDragRegionPosition = Apptitlebar.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

				Windows.Graphics.RectInt32[] dragRects = new Windows.Graphics.RectInt32[2];

				for (int i = 0; i < 2; i++)
				{
					dragRects[i] = new Windows.Graphics.RectInt32
					{
						X = (int)((customDragRegionPosition.X + (i * Apptitlebar.ActualWidth / 2)) * scaleAdjustment),
						Y = (int)(customDragRegionPosition.Y * scaleAdjustment),
						Height = (int)((Apptitlebar.ActualHeight - customDragRegionPosition.Y) * scaleAdjustment),
						Width = (int)(Apptitlebar.ActualWidth / 2 * scaleAdjustment)
					};
				}

				appWindow.TitleBar?.SetDragRectangles(dragRects);
			}
			catch (Exception)
			{
				throw;
			}
		}

		private void Apptitlebar_LayoutUpdated(object sender, object e)
		{
			double scaleAdjustment = GetScaleAdjustment();
			Apptitlebar.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
			Windows.Foundation.Point customDragRegionPosition = Apptitlebar.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

			List<Windows.Graphics.RectInt32> dragRectsList = new();

			for (int i = 0; i < 2; i++)
			{
				Windows.Graphics.RectInt32 dragRect = new()
				{
					X = (int)((customDragRegionPosition.X + (i * Apptitlebar.ActualWidth / 2)) * scaleAdjustment),
					Y = (int)(customDragRegionPosition.Y * scaleAdjustment),
					Height = (int)((Apptitlebar.ActualHeight - customDragRegionPosition.Y) * scaleAdjustment),
					Width = (int)(Apptitlebar.ActualWidth / 2 * scaleAdjustment)
				};

				dragRectsList.Add(dragRect);
			}

			Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

			appWindow.TitleBar?.SetDragRectangles(dragRects);
		}

		private void QRCodeTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (QRCodeTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
			{
				string selectedType = selectedItem.Content?.ToString() ?? string.Empty;

				if (WifiInputs != null)
					WifiInputs.Visibility = selectedType == "Wifi" ? Visibility.Visible : Visibility.Collapsed;

				if (TextInput != null)
					TextInput.Visibility = (selectedType == "Text" || selectedType == "URL") ? Visibility.Visible : Visibility.Collapsed;

				if (PhoneInput != null)
					PhoneInput.Visibility = selectedType == "Phone" ? Visibility.Visible : Visibility.Collapsed;

				if (CreateQRButton != null)
					CreateQRButton.Visibility = selectedType == "URL" ? Visibility.Collapsed : Visibility.Visible;

				if (selectedType == "URL" && TextInput != null)
				{
					TextInput.Visibility = Visibility.Collapsed;
				}
			}
		}

		private double GetScaleAdjustment()
		{
			nint hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
			nint hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

			// Get the effective DPI for the monitor
			_ = Windowing.GetDpiForMonitor(hMonitor, Windowing.Monitor_DPI_Type.MDT_Effective_DPI, out uint dpiX, out uint dpiY);

			// Calculate the average DPI scaling factor
			double scaleX = dpiX / 96.0;
			double scaleY = dpiY / 96.0;

			// Depending on your UI, you may want to return the average or just one axis
			return (scaleX + scaleY) / 2.0; // Average of X and Y scaling
		}

		private void Tabs_AddTabButtonClick(TabView sender, object args)
		{
			if (sender.TabItems.Count < maxTabItems)
			{
				_tabManager.CreateNewTab(typeof(NewTab), null, false);
			}
		}

		private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			if (args.Tab is GraphiteTabViewItem tabToClose)
			{
				_tabManager.CloseTab(tabToClose);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow(_currentUser);
			settingsWindow.Activate();
		}		
	}
}

