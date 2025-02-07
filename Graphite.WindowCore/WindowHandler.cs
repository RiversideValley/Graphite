using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Graphite.WindowCore
{
	public interface IWindowHandler
	{
		Window MainWindow { get; }
		IntPtr Hwnd { get; }
		AppWindow AppWindow { get; }
		TitleBar TitleBar { get; }
		void Initialize(Window window);
		void SetWindowSize(int width, int height);
		void SetWindowPosition(int x, int y);
		void CenterOnScreen();
		void Maximize();
		void Minimize();
		void Restore();
		void SetWindowStyle(WindowStyle style);
		void SetWindowBackdrop(BackdropType backdropType);
		void SetIcon(string iconPath);
		void SetTitle(string title);
		Task<StorageFile> PickSaveFileAsync(string suggestedFileName = null);
		Task<StorageFile> PickOpenFileAsync(params string[] fileTypes);
		void EnableDragToMove();
		void SaveWindowPosition();
		void RestoreWindowPosition();
		void Cleanup();
	}

	public enum WindowStyle
	{
		Default,
		NoResize,
		NoBorder,
		FullScreen
	}

	public enum BackdropType
	{
		Default,
		Mica,
		MicaAlt,
		Acrylic,
		Transparent
	}

	public class WindowHandler : IWindowHandler, IDisposable
	{
		public Window MainWindow { get; private set; }
		public IntPtr Hwnd { get; private set; }
		public AppWindow AppWindow { get; private set; }
		public TitleBar TitleBar { get; private set; }

		private SystemBackdropConfiguration _backdropConfiguration;
		private MicaController _micaController;
		private DesktopAcrylicController _acrylicController;
		private WindowsSystemDispatcherQueueHelper _wsdqHelper;
		private const string WindowPositionKey = "WindowPosition";
		private bool _disposedValue;

		public WindowHandler()
		{
			_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
			_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();
		}

		public void Initialize(Window window)
		{
			MainWindow = window;
			Hwnd = GetWindowHandle(window);
			AppWindow = GetAppWindow(window);
			TitleBar = new TitleBar(this);
			TitleTop();
			SetupDefaultProperties();
			MainWindow.Closed += MainWindow_Closed;
		}

		private void TitleTop()
		{
			if (!AppWindowTitleBar.IsCustomizationSupported())
			{
				throw new Exception("Unsupported OS version.");
			}

			AppWindowTitleBar titleBar = AppWindow.TitleBar;
			titleBar.ExtendsContentIntoTitleBar = true;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = titleBar.ButtonBackgroundColor =
				titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor =
				titleBar.ButtonHoverBackgroundColor = btnColor;
		}

		private void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			Cleanup();
		}

		private void SetupDefaultProperties()
		{
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			SetWindowBackdrop(BackdropType.Mica);
			EnableDragToMove();
		}

		public void SetWindowSize(int width, int height)
		{
			AppWindow.Resize(new SizeInt32(width, height));
		}

		public void SetWindowPosition(int x, int y)
		{
			AppWindow.Move(new PointInt32(x, y));
		}

		public void CenterOnScreen()
		{
			var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
			if (displayArea != null)
			{
				var centerX = (displayArea.WorkArea.Width - AppWindow.Size.Width) / 2;
				var centerY = (displayArea.WorkArea.Height - AppWindow.Size.Height) / 2;
				AppWindow.Move(new PointInt32(centerX, centerY));
			}
		}

		public void Maximize()
		{
			AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
		}

		public void Minimize()
		{
			AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
			AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
		}

		public void Restore()
		{
			AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
		}

		public void SetWindowStyle(WindowStyle style)
		{
			switch (style)
			{
				case WindowStyle.NoResize:
					AppWindow.ResizeClient(new SizeInt32(AppWindow.Size.Width, AppWindow.Size.Height));
					break;
				case WindowStyle.NoBorder:
					AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
					AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
					break;
				case WindowStyle.FullScreen:
					AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
					break;
				default:
					AppWindow.SetPresenter(AppWindowPresenterKind.Default);
					break;
			}
		}

		public void SetWindowBackdrop(BackdropType backdropType)
		{
			if (_backdropConfiguration == null)
			{
				_backdropConfiguration = new SystemBackdropConfiguration();
			}

			switch (backdropType)
			{
				case BackdropType.Mica:
					SetMicaBackdrop(false);
					break;
				case BackdropType.MicaAlt:
					SetMicaBackdrop(true);
					break;
				case BackdropType.Acrylic:
					SetAcrylicBackdrop();
					break;
				case BackdropType.Transparent:
					SetTransparentBackdrop();
					break;
				default:
					RemoveBackdrop();
					break;
			}
		}

		private void SetMicaBackdrop(bool useAlt)
		{
			_micaController?.Dispose();
			_micaController = new MicaController();
			_micaController.Kind = useAlt ? MicaKind.BaseAlt : MicaKind.Base;
			_micaController.AddSystemBackdropTarget(MainWindow.As<ICompositionSupportsSystemBackdrop>());
			_micaController.SetSystemBackdropConfiguration(_backdropConfiguration);
			MainWindow.SystemBackdrop = new MicaBackdrop() { Kind = useAlt ? MicaKind.BaseAlt : MicaKind.Base };
		}

		private void SetAcrylicBackdrop()
		{
			_acrylicController?.Dispose();
			_acrylicController = new DesktopAcrylicController();
			_acrylicController.AddSystemBackdropTarget(MainWindow.As<ICompositionSupportsSystemBackdrop>());
			_acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
			MainWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
		}

		private void SetTransparentBackdrop()
		{
			MainWindow.SystemBackdrop = null;
		}

		private void RemoveBackdrop()
		{
			if (MainWindow != null)
			{
				MainWindow.Closed -= MainWindow_Closed;
			}

			if (_micaController != null)
			{
				_micaController.Dispose();
				_micaController = null;
			}

			if (_acrylicController != null)
			{
				_acrylicController.Dispose();
				_acrylicController = null;
			}

			if (MainWindow != null)
			{
				MainWindow.SystemBackdrop = null;
			}
		}

		public void SetIcon(string iconPath)
		{
			AppWindow.SetIcon(iconPath);
		}

		public void SetTitle(string title)
		{
			AppWindow.Title = title;
		}

		public async Task<StorageFile> PickSaveFileAsync(string suggestedFileName = null)
		{
			var savePicker = new Windows.Storage.Pickers.FileSavePicker();
			WinRT.Interop.InitializeWithWindow.Initialize(savePicker, Hwnd);

			savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
			savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

			if (!string.IsNullOrEmpty(suggestedFileName))
			{
				savePicker.SuggestedFileName = suggestedFileName;
			}

			return await savePicker.PickSaveFileAsync();
		}

		public async Task<StorageFile> PickOpenFileAsync(params string[] fileTypes)
		{
			var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
			WinRT.Interop.InitializeWithWindow.Initialize(openPicker, Hwnd);

			openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
			foreach (var fileType in fileTypes)
			{
				openPicker.FileTypeFilter.Add(fileType);
			}

			return await openPicker.PickSingleFileAsync();
		}

		public void EnableDragToMove()
		{
			MainWindow.ExtendsContentIntoTitleBar = true;
			MainWindow.SetTitleBar(TitleBar.GetTitleBarElement());
		}

		public void SaveWindowPosition()
		{
			var position = AppWindow.Position;
			var size = AppWindow.Size;
			ApplicationData.Current.LocalSettings.Values[WindowPositionKey] = $"{position.X},{position.Y},{size.Width},{size.Height}";
		}

		public void RestoreWindowPosition()
		{
			if (ApplicationData.Current.LocalSettings.Values.TryGetValue(WindowPositionKey, out object positionData))
			{
				var parts = ((string)positionData).Split(',');
				if (parts.Length == 4 &&
					int.TryParse(parts[0], out int x) &&
					int.TryParse(parts[1], out int y) &&
					int.TryParse(parts[2], out int width) &&
					int.TryParse(parts[3], out int height))
				{
					AppWindow.Move(new PointInt32(x, y));
					AppWindow.Resize(new SizeInt32(width, height));
				}
			}
		}

		public void Cleanup()
		{
			if (_disposedValue) return;

			RemoveBackdrop();
			SaveWindowPosition();

			_disposedValue = true;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					Cleanup();
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private static IntPtr GetWindowHandle(Window window)
		{
			var windowNative = window.As<IWindowNative>();
			return windowNative.WindowHandle;
		}

		private static AppWindow GetAppWindow(Window window)
		{
			var windowId = Win32Interop.GetWindowIdFromWindow(GetWindowHandle(window));
			return AppWindow.GetFromWindowId(windowId);
		}
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
	internal interface IWindowNative
	{
		IntPtr WindowHandle { get; }
	}

	public class WindowsSystemDispatcherQueueHelper
	{
		[StructLayout(LayoutKind.Sequential)]
		struct DispatcherQueueOptions
		{
			internal int dwSize;
			internal int threadType;
			internal int apartmentType;
		}

		[DllImport("CoreMessaging.dll")]
		private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

		object m_dispatcherQueueController = null;
		public void EnsureWindowsSystemDispatcherQueueController()
		{
			if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
			{
				// one already exists, so we'll just use it.
				return;
			}

			if (m_dispatcherQueueController == null)
			{
				DispatcherQueueOptions options;
				options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
				options.threadType = 2;    // DQTYPE_THREAD_CURRENT
				options.apartmentType = 2; // DQTAT_COM_STA

				CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
			}
		}
	}
}

