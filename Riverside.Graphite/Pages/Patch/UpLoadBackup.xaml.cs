using Riverside.Graphite.Runtime.Helpers;
using FireBrowserWinUi3.Services.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3.Pages.Patch
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class UpLoadBackup : Window
	{
		public UploadBackupViewModel ViewModel { get; }
		public static UpLoadBackup Instance { get; set; }
		public UpLoadBackup()
		{
			Instance = this;
			ViewModel = new();
			InitializeComponent();

			_ = ShowWindowSlide().ConfigureAwait(false);
		}


		private DispatcherTimer timer;
		private int bounceCount = 0;
		private int screenWidth, screenHeight, windowWidth, windowHeight;

		private async Task ShowWindowSlide()
		{
			Windows.Graphics.SizeInt32? desktop = await Windowing.SizeWindow();
			// Get screen dimensions
			screenWidth = desktop.Value.Width;
			screenHeight = desktop.Value.Height;

			// Get window dimensions
			windowWidth = AppWindow.Size.Width;
			windowHeight = AppWindow.Size.Height;

			nint windowHandle = WindowNative.GetWindowHandle(this);

			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(40); // Adjust the interval for smoother animation
			timer.Tick += (s, args) => AnimateWindow(windowHandle);
			timer.Start();


		}

		private void AnimateWindow(IntPtr hwnd)
		{
			bounceCount++;

			// Calculate the center position
			int centerX = (screenWidth - windowWidth) / 2;
			int startY = -windowHeight; // Start above the screen
			int endY = (screenHeight - windowHeight) / 2;

			// Calculate the new Y position
			int newY = startY + (bounceCount * 50);
			if (bounceCount > 10) // Bounce effect after sliding in
			{
				newY = endY - (int)(Math.Sin((bounceCount - 10) * 0.3) * 50);
			}

			// Move the window to the new position
			_ = Windowing.SetWindowPos(hwnd, IntPtr.Zero, centerX, newY, 0, 0, Windowing.SWP_NOZORDER | Windowing.SWP_NOSIZE);

			// Stop the animation after a while
			if (bounceCount > 30)
			{
				timer.Stop();
				// Ensure window ends at the center position
				_ = Windowing.SetWindowPos(hwnd, IntPtr.Zero, centerX, endY, 0, 0, Windowing.SWP_NOZORDER | Windowing.SWP_NOSIZE);

			}
		}
		private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			DataPackage dataPackage = new();
			dataPackage.SetText(ViewModel.FileSelected.BlobUrl);
			Clipboard.SetContent(dataPackage);
		}
	}
}
