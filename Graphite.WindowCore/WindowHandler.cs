using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel.Activation;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace Graphite.WindowCore;

public interface IWindowHandler
{
	Window MainWindow { get; }
	IntPtr Hwnd { get; }
	void OnLaunched(LaunchActivatedEventArgs args);
	void Initialize(Window window);
	void EnableMica();
	void EnableAcrylic();
	void SetWindowSize(int width, int height);
	void SetWindowPosition(int x, int y);
	void SetWindowStyle(WindowStyles style, bool enable);
	void SetWindowExStyle(WindowExStyles exStyle, bool enable);
	void SetWindowTransparency(byte alpha);
	Window GetMainWindow();
	TitleBar TitleBar { get; }
}

public class WindowHandler : IWindowHandler
{
	public Window MainWindow { get; private set; }
	private AppWindow _appWindow;
	public IntPtr Hwnd { get; private set; }
	public TitleBar TitleBar { get; private set; }

	public WindowHandler() { }

	public void Initialize(Window window)
	{
		MainWindow = window;
		Hwnd = GetWindowHandle(window);
		_appWindow = GetAppWindow(window);
		TitleBar = new TitleBar(this);

		SetupWindowProperties();
	}

	public void OnLaunched(LaunchActivatedEventArgs args)
	{
		MainWindow = new Window();
		Initialize(MainWindow);
		MainWindow.Activate();

		ProcessLaunchActivation();
	}

	private void ProcessLaunchActivation()
	{
		AppInstance currentInstance = AppInstance.GetCurrent();
		if (currentInstance.IsCurrent)
		{
			AppActivationArguments activationArgs = currentInstance.GetActivatedEventArgs();
			if (activationArgs != null)
			{
				ExtendedActivationKind extendedKind = activationArgs.Kind;
				if (extendedKind == ExtendedActivationKind.AppNotification)
				{
					var notificationActivatedEventArgs = activationArgs.Data as AppNotificationActivatedEventArgs;
					ProcessNotification(notificationActivatedEventArgs);
				}
			}
		}
	}

	private void ProcessNotification(AppNotificationActivatedEventArgs args)
	{
		// Implement your notification processing logic here
		// For example:
		// NotificationManager.ProcessLaunchActivationArgs(args);
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

	private void SetupWindowProperties()
	{
		_appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		EnableMica();
	}

	public void EnableMica()
	{
		int micaValue = 1;
		if (NativeMethods.DwmSetWindowAttribute(
			Hwnd,
			NativeMethods.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT,
			ref micaValue,
			sizeof(int)) != 0)
		{
			// Fallback to Acrylic if Mica is not available
			EnableAcrylic();
		}
	}

	public void EnableAcrylic()
	{
		var accent = new NativeMethods.ACCENT_POLICY
		{
			AccentState = NativeMethods.ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
			GradientColor = 0x99FFFFFF // Adjust color and opacity as needed
		};

		var accentStructSize = Marshal.SizeOf(accent);
		var accentPtr = Marshal.AllocHGlobal(accentStructSize);
		Marshal.StructureToPtr(accent, accentPtr, false);

		var data = new NativeMethods.WINDOWCOMPOSITIONATTRIBDATA
		{
			Attrib = NativeMethods.WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
			pvData = accentPtr,
			cbData = accentStructSize
		};

		NativeMethods.SetWindowCompositionAttribute(Hwnd, ref data);

		Marshal.FreeHGlobal(accentPtr);
	}

	public void SetWindowSize(int width, int height)
	{
		_appWindow.Resize(new SizeInt32(width, height));
	}

	public void SetWindowPosition(int x, int y)
	{
		_appWindow.Move(new PointInt32(x, y));
	}

	public void SetWindowStyle(WindowStyles style, bool enable)
	{
		var currentStyle = (WindowStyles)NativeMethods.GetWindowLong(Hwnd, NativeMethods.GWL_STYLE);

		if (enable)
			currentStyle |= style;
		else
			currentStyle &= ~style;

		NativeMethods.SetWindowLong(Hwnd, NativeMethods.GWL_STYLE, (int)currentStyle);
	}

	public void SetWindowExStyle(WindowExStyles exStyle, bool enable)
	{
		var currentExStyle = (WindowExStyles)NativeMethods.GetWindowLong(Hwnd, NativeMethods.GWL_EXSTYLE);

		if (enable)
			currentExStyle |= exStyle;
		else
			currentExStyle &= ~exStyle;

		NativeMethods.SetWindowLong(Hwnd, NativeMethods.GWL_EXSTYLE, (int)currentExStyle);
	}

	public void SetWindowTransparency(byte alpha)
	{
		SetWindowExStyle(WindowExStyles.WS_EX_LAYERED, true);
		NativeMethods.SetLayeredWindowAttributes(Hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
	}

	public Window GetMainWindow()
	{
		return MainWindow;
	}
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
internal interface IWindowNative
{
	IntPtr WindowHandle { get; }
}

[Flags]
public enum WindowStyles : uint
{
	WS_OVERLAPPED = 0x00000000,
	WS_POPUP = 0x80000000,
	WS_CHILD = 0x40000000,
	WS_MINIMIZE = 0x20000000,
	WS_VISIBLE = 0x10000000,
	WS_DISABLED = 0x08000000,
	WS_CLIPSIBLINGS = 0x04000000,
	WS_CLIPCHILDREN = 0x02000000,
	WS_MAXIMIZE = 0x01000000,
	WS_CAPTION = 0x00C00000,
	WS_BORDER = 0x00800000,
	WS_DLGFRAME = 0x00400000,
	WS_VSCROLL = 0x00200000,
	WS_HSCROLL = 0x00100000,
	WS_SYSMENU = 0x00080000,
	WS_THICKFRAME = 0x00040000,
	WS_GROUP = 0x00020000,
	WS_TABSTOP = 0x00010000,
	WS_MINIMIZEBOX = 0x00020000,
	WS_MAXIMIZEBOX = 0x00010000,
}

[Flags]
public enum WindowExStyles : uint
{
	WS_EX_DLGMODALFRAME = 0x00000001,
	WS_EX_NOPARENTNOTIFY = 0x00000004,
	WS_EX_TOPMOST = 0x00000008,
	WS_EX_ACCEPTFILES = 0x00000010,
	WS_EX_TRANSPARENT = 0x00000020,
	WS_EX_MDICHILD = 0x00000040,
	WS_EX_TOOLWINDOW = 0x00000080,
	WS_EX_WINDOWEDGE = 0x00000100,
	WS_EX_CLIENTEDGE = 0x00000200,
	WS_EX_CONTEXTHELP = 0x00000400,
	WS_EX_RIGHT = 0x00001000,
	WS_EX_LEFT = 0x00000000,
	WS_EX_RTLREADING = 0x00002000,
	WS_EX_LTRREADING = 0x00000000,
	WS_EX_LEFTSCROLLBAR = 0x00004000,
	WS_EX_RIGHTSCROLLBAR = 0x00000000,
	WS_EX_CONTROLPARENT = 0x00010000,
	WS_EX_STATICEDGE = 0x00020000,
	WS_EX_APPWINDOW = 0x00040000,
	WS_EX_LAYERED = 0x00080000,
	WS_EX_NOINHERITLAYOUT = 0x00100000,
	WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
	WS_EX_LAYOUTRTL = 0x00400000,
	WS_EX_COMPOSITED = 0x02000000,
	WS_EX_NOACTIVATE = 0x08000000
}