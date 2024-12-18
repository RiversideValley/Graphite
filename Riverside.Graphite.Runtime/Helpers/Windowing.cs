using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Graphics;
using WinRT.Interop;

namespace Riverside.Graphite.Runtime.Helpers;
public class WindowBounce(Window inWindow)
{
	private DispatcherTimer timer;
	private int bounceCount = 0;
	private int screenWidth, screenHeight, windowWidth, windowHeight;


	public async Task ShowWindowBounce()
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(inWindow);
		WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
		AppWindow appWindow = AppWindow.GetFromWindowId(wndId);

		Windows.Graphics.SizeInt32? desktop = await Windowing.SizeWindow();
		// Get screen dimensions
		screenWidth = desktop.Value.Width;
		screenHeight = desktop.Value.Height;

		// Get window dimensions
		windowWidth = appWindow.Size.Width;
		windowHeight = appWindow.Size.Height;

		timer = new DispatcherTimer();
		timer.Interval = TimeSpan.FromMilliseconds(40); // Adjust the interval for smoother animation
		timer.Tick += (s, args) => AnimateWindow(hWnd);
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
}
public class Windowing
{
	public enum Monitor_DPI_Type : int
	{
		MDT_Effective_DPI = 0,
		MDT_Angular_DPI = 1,
		MDT_Raw_DPI = 2,
		MDT_Default_DPI = MDT_Effective_DPI,
	}
	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool AnimateWindow(IntPtr hwnd, int dwTime, int dwFlags);




	public const int AW_HOR_POSITIVE = 0x0001;
	public const int AW_HOR_NEGATIVE = 0x0002;
	public const int AW_VER_POSITIVE = 0x0004;
	public const int AW_VER_NEGATIVE = 0x0008;
	public const int AW_CENTER = 0x0010;
	public const int AW_SLIDE = 0x00040000;
	public const int AW_ACTIVATE = 0x20000;
	public const int AW_BLEND = 0x80000;

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr CreateWindowEx(
		int dwExStyle,
		string lpClassName,
		string lpWindowName,
		int dwStyle,
		int x,
		int y,
		int nWidth,
		int nHeight,
		IntPtr hWndParent,
		IntPtr hMenu,
		IntPtr hInstance,
		IntPtr lpParam);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DestroyWindow(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetMessage([Out] out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool TranslateMessage([In] ref MSG lpMsg);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

	[StructLayout(LayoutKind.Sequential)]
	public struct MSG
	{
		public IntPtr hwnd;
		public uint message;
		public UIntPtr wParam;
		public IntPtr lParam;
		public uint time;
		public POINT pt;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int x;
		public int y;
	}

	public const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
	public const int CW_USEDEFAULT = unchecked((int)0x80000000);



	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
	public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern bool CloseWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

	public static void FlashWindow(IntPtr hwnd)
	{
		_ = FlashWindow(hwnd, true);
	}
	public static bool AnimateWindow(IntPtr hwnd)
	{
		return AnimateWindow(hwnd, 200, AW_SLIDE | AW_ACTIVATE | AW_BLEND);
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("Shcore.dll", SetLastError = true)]
	public static extern int GetDpiForMonitor(IntPtr hmonitor, Windowing.Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWindowEnabled(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);


	static int PositionOfNewWindow = 0;
	public static void Center(Window window)
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(window);
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

		if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
			DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
		{
			PointInt32 CenteredPosition = appWindow.Position;
			CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
			CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
			appWindow.Move(CenteredPosition);
		}
	}
	public static void Center(IntPtr hWnd)
	{
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

		if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
			DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
		{
			PointInt32 CenteredPosition = appWindow.Position;
			CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
			CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
			appWindow.Move(CenteredPosition);
		}
	}
	public struct WindowInfo
	{
		public IntPtr hWnd;          // Handle to the window
		public IntPtr ParentWinId;   // Handle to the parent window
		public string Tracker;       // Tracker string to identify or track the window

		public WindowInfo(IntPtr windowHandle, IntPtr parentWindowHandle, string tracker)
		{
			hWnd = windowHandle;
			ParentWinId = parentWindowHandle;
			Tracker = tracker;
		}

		public (int Width, int Height) Size => GetWindowSize(hWnd);

		private (int Width, int Height) GetWindowSize(IntPtr hWnd)
		{
			RECT rect;
			if (GetWindowRect(hWnd, out rect))
			{
				int width = rect.right - rect.left;
				int height = rect.bottom - rect.top;
				return (width, height);
			}
			else
			{
				throw new InvalidOperationException("Failed to get window size.");
			}
		}

		public override string ToString()
		{
			var size = Size;
			return $"Window Handle: {hWnd}, Parent Window ID: {ParentWinId}, Tracker: {Tracker}, Size: Width={size.Width}, Height={size.Height}";
		}
	}


	public static SizeInt32 GetTheSizeofWindow(IntPtr hWnd)
	{
		SizeInt32 size = new SizeInt32();

		RECT rect;
		if (GetWindowRect(hWnd, out rect))
		{
			size.Width = rect.right - rect.left;
			size.Height = rect.bottom - rect.top;
		}

		return size;
	}

	public static WindowInfo Commander { get; private set; }
	public static async void AllowNonOverlappingWindow(Window inWindow)
	{
		var dlg = GetAppWindow(inWindow);
		var winSize = await SizeWindow();
		var hWnd = WindowNative.GetWindowHandle(inWindow);
		var parent = GetForegroundWindow();


		if (dlg is not null)
		{
			var winParentId = Win32Interop.GetWindowIdFromWindow(parent);

			Microsoft.UI.Windowing.DisplayArea displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(winParentId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);

			if (displayArea is not null)
			{
				var CenteredPosition = dlg.Position;
				CenteredPosition.X = ((displayArea.WorkArea.Width - dlg.Size.Width - PositionOfNewWindow));
				// allow for the titlebar.
				CenteredPosition.Y = ((displayArea.WorkArea.Height - dlg.Size.Height + 55));
				dlg.Move(CenteredPosition);

				Commander = new WindowInfo(hWnd, parent, GetWindowTitle(parent));
				var parentSize = GetTheSizeofWindow(parent);

				RECT rect;
				var parentPosition = GetWindowRect(parent, out rect);

				if (((rect.left) + (CenteredPosition.X + dlg.Size.Width)) >= (displayArea.WorkArea.Width - dlg.Size.Width))
				{
					ChangeWindowPosition(parent, -4, 0);

					var newSize = new SizeInt32((int)displayArea.WorkArea.Width - dlg.Size.Width - PositionOfNewWindow, parentSize.Height);

					ChangeWindowSize(parent, newSize.Width, newSize.Height);
				}
			}
		}
		PositionOfNewWindow += 15;
	}

	public static bool ChangeWindowPosition(IntPtr hWnd, int x, int y)
	{
		return SetWindowPos(hWnd, HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
	}
	public static bool ChangeWindowSize(IntPtr hWnd, int width, int height)
	{
		return SetWindowPos(hWnd, HWND_TOP, 0, 0, width, height, SWP_NOMOVE | SWP_NOZORDER | SWP_SHOWWINDOW);
	}
	public static string GetWindowTitle(IntPtr hWnd)
	{
		int length = GetWindowTextLength(hWnd);
		if (length == 0)
		{
			return string.Empty;
		}

		StringBuilder title = new StringBuilder(length + 1);
		GetWindowText(hWnd, title, title.Capacity);
		return title.ToString();
	}

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern int GetWindowTextLength(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	public static Window FromIntPtr(IntPtr hwnd)
	{
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
		_ = AppWindow.GetFromWindowId(windowId);
		Window window = new();
		InitializeWithWindow.Initialize(window, nint.Parse(hwnd.ToInt64().ToString()));
		return window;
	}


	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

	public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
	public static bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
	{
		StringBuilder sb = new(256);
		_ = GetWindowText(hWnd, sb, sb.Capacity);
		Console.WriteLine(sb.ToString());
		return true;
	}
	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

	public static List<IntPtr> FindWindowsByName(string windowName)
	{
		List<IntPtr> windows = new();

		_ = EnumWindows((hWnd, lParam) =>
		{
			StringBuilder sb = new(256);
			_ = GetWindowText(hWnd, sb, sb.Capacity);
			if (sb.ToString().Contains(windowName))
			{
				windows.Add(hWnd);
			}
			return true; // Continue enumeration
		}, IntPtr.Zero);

		return windows;
	}

	public static void CascadeWindows(List<IntPtr> windows)
	{
		// we'll assume titlebar is default at 48 height . 
		int offset = 48;
		int currentX = 0;
		int currentY = 0;

		foreach (nint hWnd in windows)
		{
			int width, height;

			if (GetWindowRect(hWnd, out RECT rect))
			{
				width = rect.right - rect.left;
				height = rect.bottom - rect.top;
				if (currentX == 0)
				{
					currentX = rect.left;
				}

				if (currentY == 0)
				{
					currentY = rect.top;
				}

				_ = MoveWindow(hWnd, currentX, currentY, width, height, true);
				currentX += offset;
				currentY += offset;
			}
		}
	}
	public static IntPtr[] GetChildWindows(IntPtr hwndParent)
	{
		List<nint> childWindows = new();
		EnumWindowsProc callback = (hWnd, lParam) =>
		{
			childWindows.Add(hWnd);
			return true; // Continue enumeration
		};

		_ = EnumChildWindows(hwndParent, callback, IntPtr.Zero);
		return childWindows.ToArray();
	}
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool UpdateWindow(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern uint CascadeWindows(IntPtr hwndParent, uint wHow, ref RECT lpRect, uint cKids, IntPtr[] lpKids);

	public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	public static void CascadeAllWindows(nint win)
	{
		nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);

		Windowing.RECT rect = new() { left = 0, top = 0, right = 800, bottom = 600 };
		IntPtr[] childWindows = Windowing.GetChildWindows(hwnd);

		_ = Windowing.CascadeWindows(hwnd, 0, ref rect, (uint)childWindows.Length, childWindows);
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}
	public const int GWLP_WNDPROC = -4;

	public static uint SWP_NOSIZE = 0x0001;
	public static uint SWP_NOMOVE = 0x0002;
	public static uint SWP_NOZORDER = 0x0004;
	public static uint SWP_NOREDRAW = 0x0008;
	public static uint SWP_NOACTIVATE = 0x0010;
	public static uint SWP_FRAMECHANGED = 0x0020;
	public static uint SWP_SHOWWINDOW = 0x0040;
	public static uint SWP_HIDEWINDOW = 0x0080;
	public static uint SWP_NOCOPYBITS = 0x0100;
	public static uint SWP_NOOWNERZORDER = 0x0200;
	public static uint SWP_NOSENDCHANGING = 0x0400;
	public static readonly IntPtr HWND_TOP = new(0);
	public static readonly IntPtr HWND_TOPMOST = new(-1);
	public static readonly IntPtr HWND_BOTTOM = new(1);
	public const uint WM_CLOSE = 0x0010;
	public enum WindowShowStyle : uint
	{
		SW_HIDE = 0,

		SW_SHOWNORMAL = 1,

		SW_SHOWMINIMIZED = 2,

		SW_SHOWMAXIMIZED = 3,

		SW_MAXIMIZE = 3,

		SW_SHOWNOACTIVATE = 4,

		SW_SHOW = 5,

		SW_MINIMIZE = 6,

		SW_SHOWMINNOACTIVE = 7,

		SW_SHOWNA = 8,

		SW_RESTORE = 9,

		SW_SHOWDEFAULT = 10,

		SW_FORCEMINIMIZE = 11,
	}

	public static void HideWindow(IntPtr hWnd)
	{
		_ = ShowWindow(hWnd, WindowShowStyle.SW_HIDE);
	}
	public static void MaximizeWindow(IntPtr hWnd)
	{
		_ = ShowWindow(hWnd, WindowShowStyle.SW_MAXIMIZE);
	}

	public static async Task<SizeInt32?> SizeWindow()
	{
		DeviceInformationCollection displayList = await DeviceInformation.FindAllAsync
						  (DisplayMonitor.GetDeviceSelector());

		if (!displayList.Any())
		{
			return null;
		}

		DisplayMonitor monitorInfo = await DisplayMonitor.FromInterfaceIdAsync(displayList[0].Id);

		SizeInt32 winSize = new();

		if (monitorInfo == null)
		{
			winSize.Width = 800;
			winSize.Height = 1200;
		}
		else
		{
			winSize.Height = monitorInfo.NativeResolutionInRawPixels.Height;
			winSize.Width = monitorInfo.NativeResolutionInRawPixels.Width;
		}

		return winSize;
	}

	public static Task DialogWindow(Window window)
	{
		try
		{
			IntPtr hWnd = WindowNative.GetWindowHandle(window);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

			if (appWindow != null)
			{
				// Remove default title bar
				appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
				appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
				appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
				appWindow.TitleBar.ButtonForegroundColor = Colors.White;
				appWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;

				// Set window size
				SizeInt32 size = new(600, 900);
				appWindow.Resize(size);

				// Remove default window chrome
				OverlappedPresenter presenter = appWindow.Presenter as OverlappedPresenter;
				if (presenter != null)
				{
					presenter.IsResizable = false;
					presenter.SetBorderAndTitleBar(true, false);
				}
			}
		}
		catch (Exception ex)
		{
			return Task.FromException(ex);
		}

		return Task.CompletedTask;
	}
	public static AppWindow GetAppWindow(Window window)
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(window);
		WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
		return AppWindow.GetFromWindowId(wndId);
	}
}