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
		void ShowMessageDialog(string title, string message);
		void EnableDragToMove();
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
		Acrylic,
		Transparent
	}

	public class WindowHandler : IWindowHandler
	{
		public Window MainWindow { get; private set; }
		public IntPtr Hwnd { get; private set; }
		public AppWindow AppWindow { get; private set; }
		public TitleBar TitleBar { get; private set; }

		private SystemBackdropConfiguration _backdropConfiguration;
		private MicaController _micaController;
		private DesktopAcrylicController _acrylicController;

		public WindowHandler() { }

		public void Initialize(Window window)
		{
			MainWindow = window;
			Hwnd = GetWindowHandle(window);
			AppWindow = GetAppWindow(window);
			TitleBar = new TitleBar(this);
	
			SetupDefaultProperties();
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
			AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
		}

		public void Minimize()
		{
			AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
		}

		public void Restore()
		{
			AppWindow.SetPresenter(AppWindowPresenterKind.Default);
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
					SetMicaBackdrop();
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

		private void SetMicaBackdrop()
		{
			_micaController = new MicaController();
			_micaController.AddSystemBackdropTarget(MainWindow.As<ICompositionSupportsSystemBackdrop>());
			_micaController.SetSystemBackdropConfiguration(_backdropConfiguration);
		}

		private void SetAcrylicBackdrop()
		{
			_acrylicController = new DesktopAcrylicController();
			_acrylicController.AddSystemBackdropTarget(MainWindow.As<ICompositionSupportsSystemBackdrop>());
			_acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
		}

		private void SetTransparentBackdrop()
		{
			MainWindow.SystemBackdrop = new TransparentBackdrop();
		}

		private void RemoveBackdrop()
		{
			_micaController?.Dispose();
			_acrylicController?.Dispose();
			_micaController = null;
			_acrylicController = null;
			MainWindow.SystemBackdrop = null;
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

		public void ShowMessageDialog(string title, string message)
		{
			var dialog = new ContentDialog
			{
				Title = title,
				Content = message,
				CloseButtonText = "OK"
			};

			dialog.XamlRoot = MainWindow.Content.XamlRoot;
			_ = dialog.ShowAsync();
		}

		public void EnableDragToMove()
		{
			MainWindow.ExtendsContentIntoTitleBar = true;
			MainWindow.SetTitleBar(TitleBar.GetTitleBarElement());
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

	public class TransparentBackdrop : SystemBackdrop
	{
		public TransparentBackdrop()
		{
			//later a transparent handler for window brush #000000 or other static for they brush opcapacity 45%
		}
	}
}

