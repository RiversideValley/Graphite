using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Graphics;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;

namespace Riverside.Graphite.Helpers
{
	public class WindowManagerScale
	{
		public static void ConfigureWindowAppearance(Window window)
		{
			if (window is null)
			{
				Debug.WriteLine("Window is null. Cannot configure window appearance.");
				return;
			}

			try
			{
				var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
				var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
				var appWindow = AppWindow.GetFromWindowId(windowId);

				if (appWindow != null)
				{
					// Get all display monitors
					var displays = GetAllDisplays();

					// Find the display containing the window
					var windowDisplay = FindDisplayContainingWindow(displays, appWindow);

					// Calculate optimal window size based on display properties
					(int width, int height) = CalculateOptimalWindowSize(windowDisplay);

					// Ensure minimum window size
					width = Math.Max(width, 600);
					height = Math.Max(height, 500);

					// Calculate position to center the window on the current display
					int left = windowDisplay.WorkArea.X + (windowDisplay.WorkArea.Width - width) / 2;
					int top = windowDisplay.WorkArea.Y + (windowDisplay.WorkArea.Height - height) / 2;

					// Set window size and position
					appWindow.MoveAndResize(new RectInt32(left, top, width, height));

					// Configure window properties
					ConfigureWindowProperties(appWindow);

					Debug.WriteLine($"Window configured: Size({width}x{height}), Position({left},{top}), Display({windowDisplay.DisplayId})");
				}
				else
				{
					Debug.WriteLine("AppWindow is null. Cannot configure window appearance.");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error configuring window appearance: {ex.Message}");
			}
		}

		private static List<DisplayInfo> GetAllDisplays()
		{
			var displays = new List<DisplayInfo>();
			foreach (var display in DisplayArea.FindAll())
			{
				var workArea = display.WorkArea;
				var displayInfo = new DisplayInfo
				{
					DisplayId = (uint)display.DisplayId.Value,
					WorkArea = workArea,
					DiagonalInches = CalculateDiagonalInches(workArea.Width, workArea.Height),
					IsPrimary = display.IsPrimary
				};
				displays.Add(displayInfo);
			}
			return displays;
		}

		private static DisplayInfo FindDisplayContainingWindow(List<DisplayInfo> displays, AppWindow appWindow)
		{
			var windowBounds = appWindow.Position;
			foreach (var display in displays)
			{
				if (display.WorkArea.X <= windowBounds.X && windowBounds.X < display.WorkArea.X + display.WorkArea.Width &&
					display.WorkArea.Y <= windowBounds.Y && windowBounds.Y < display.WorkArea.Y + display.WorkArea.Height)
				{
					return display;
				}
			}
			return displays.Find(d => d.IsPrimary) ?? displays[0];
		}

		private static (int width, int height) CalculateOptimalWindowSize(DisplayInfo display)
		{
			double widthPercentage, heightPercentage;

			if (display.DiagonalInches < 21)
			{
				widthPercentage = 0.9;
				heightPercentage = 0.9;
			}
			else if (display.DiagonalInches >= 34)
			{
				widthPercentage = 0.6;
				heightPercentage = 0.7;
			}
			else
			{
				// Linear interpolation for sizes between 21" and 34"
				double t = (display.DiagonalInches - 21) / (34 - 21);
				widthPercentage = 0.9 - (0.3 * t);
				heightPercentage = 0.9 - (0.2 * t);
			}

			// Adjust percentages based on resolution
			if (display.WorkArea.Height >= 2160) // 4K and above
			{
				widthPercentage *= 0.9;
				heightPercentage *= 0.9;
			}
			else if (display.WorkArea.Height >= 1440) // QHD
			{
				widthPercentage *= 0.95;
				heightPercentage *= 0.95;
			}

			int width = (int)(display.WorkArea.Width * widthPercentage);
			int height = (int)(display.WorkArea.Height * heightPercentage);

			return (width, height);
		}

		private static void ConfigureWindowProperties(AppWindow appWindow)
		{
			appWindow.MoveInZOrderAtTop();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.Title = "UserCentral";

			// Configure title bar colors
			AppWindowTitleBar titleBar = appWindow.TitleBar;
			Windows.UI.Color transparentColor = Colors.Transparent;
			Windows.UI.Color foregroundColor = Colors.LimeGreen;

			titleBar.BackgroundColor = transparentColor;
			titleBar.ForegroundColor = foregroundColor;
			titleBar.ButtonBackgroundColor = transparentColor;
			titleBar.ButtonInactiveBackgroundColor = transparentColor;

			// Set window presenter and icon
			appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
			appWindow.SetIcon("Assets\\AppTiles\\Logo.ico");
		}

		private static double CalculateDiagonalInches(int widthPixels, int heightPixels)
		{
			// Assuming a typical DPI of 96
			const double inchesPerPixel = 1.0 / 96.0;
			double widthInches = widthPixels * inchesPerPixel;
			double heightInches = heightPixels * inchesPerPixel;
			return Math.Sqrt((widthInches * widthInches) + (heightInches * heightInches));
		}

		private class DisplayInfo
		{
			public uint DisplayId { get; set; }
			public RectInt32 WorkArea { get; set; }
			public double DiagonalInches { get; set; }
			public bool IsPrimary { get; set; }
		}
	}
}