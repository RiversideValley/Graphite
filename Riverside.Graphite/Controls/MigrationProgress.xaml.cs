using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Graphics;
using WinRT.Interop;
using System.Threading.Tasks;
using Riverside.Graphite.Core;
using Riverside.Graphite.Core.Helper.Logging;

namespace Riverside.Graphite.Controls
{
	public sealed partial class MigrationProgress : Window
	{
		private AppWindow appWindow;
		private AppWindowTitleBar titleBar;

		public MigrationProgress()
		{
			this.InitializeComponent();
			this.Activated += MigrationProgress_Activated;
		}

		private void MigrationProgress_Activated(object sender, WindowActivatedEventArgs args)
		{
			if (args.WindowActivationState != WindowActivationState.Deactivated)
			{
				InitializeWindow();
				this.Activated -= MigrationProgress_Activated;
			}
		}

		public void UpdateProgress(double percentage)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				ProgressBar.Value = percentage;
			});
		}

		private void InitializeWindow()
		{
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			appWindow = AppWindow.GetFromWindowId(windowId);
			appWindow.Title = "Migrating Old Data To New System";
			appWindow.MoveAndResize(new RectInt32(500, 500, 850, 500));
			appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
			appWindow.MoveInZOrderAtTop();
			appWindow.SetIcon("Logo.ico");

			if (AppWindowTitleBar.IsCustomizationSupported())
			{
				titleBar = appWindow.TitleBar;
				titleBar.BackgroundColor = Colors.Transparent;
				titleBar.ButtonBackgroundColor = Colors.Transparent;	
				titleBar.ButtonForegroundColor	= Colors.Transparent;
				titleBar.ExtendsContentIntoTitleBar = true;
			}

			// Start the migration process after a short delay to ensure UI is fully loaded
			DispatcherQueue.TryEnqueue(async () =>
			{
				await Task.Delay(100); // Short delay to ensure UI is rendered
				await StartMgrAsync();
			});
		}

		private async Task StartMgrAsync()
		{
			var logger = new FileLogger();
			var migrationManager = new MigrationManager(logger);
			await migrationManager.PerformFullMigrationAsync();
		}
	}
}