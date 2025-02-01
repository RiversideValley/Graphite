using Graphite.Controls;
using Graphite.Pages;
using Graphite.UserSys;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using Graphite.Helpers;

namespace Graphite;

public sealed partial class HomeWindow : Window
{
	private User _currentUser;
	private AppWindow appWindow;
	private readonly int maxTabItems = 20;


	public HomeWindow(User user)
	{
		this.InitializeComponent();
		_currentUser = user;
		this.Title = $"Graphite Home Page - {_currentUser.Username}";
		Tabs.TabItems.Add(CreateNewTab(typeof(NewTab)));
		TitleTop();
	}

	public GraphiteTabViewItem CreateNewTab(Type? page = null, object param = null, int index = -1)
	{
		_ = Tabs.TabItems.Count;

		GraphiteTabViewItem newItem = new()
		{
			Header = "NewTab",
			IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource { Symbol = Symbol.Home },
			Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["FloatingTabViewItemStyle"]
		};

		ToolTipService.SetToolTip(newItem, null);

		return newItem;
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
			sender.TabItems.Add(CreateNewTab(typeof(NewTab)));
		}
	}

	private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
	{
		if (args.Tab?.Content is not Frame tabContent)
		{
			return;
		}

		_ = (sender?.TabItems?.Remove(args.Tab));
	}
}
