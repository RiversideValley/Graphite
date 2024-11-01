using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
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

namespace FireBrowserWinUi3Core.Helpers;
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



    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

    public static void FlashWindow(IntPtr hwnd)
    {
        FlashWindow(hwnd, true);
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

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    public static Window FromIntPtr(IntPtr hwnd)
    {
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var window = new Window();
        InitializeWithWindow.Initialize(window, nint.Parse(hwnd.ToInt64().ToString()))  ;
        return window;
    }


    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    public static bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
    {
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, sb.Capacity);
        Console.WriteLine(sb.ToString());
        return true;
    }
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public static List<IntPtr> FindWindowsByName(string windowName)
    {
        List<IntPtr> windows = new List<IntPtr>();

        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
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

        foreach (var hWnd in windows)
        {
            int width, height;

            if (GetWindowRect(hWnd, out RECT rect))
            {
                width = rect.right - rect.left;
                height = rect.bottom - rect.top;
                if (currentX == 0)
                    currentX = rect.left;
                if (currentY == 0)
                    currentY = rect.top;
                MoveWindow(hWnd, currentX, currentY, width, height, true);
                currentX += offset;
                currentY += offset;
            }
        }
    }
    public static IntPtr[] GetChildWindows(IntPtr hwndParent)
    {
        var childWindows = new List<IntPtr>();
        EnumWindowsProc callback = (hWnd, lParam) =>
        {
            childWindows.Add(hWnd);
            return true; // Continue enumeration
        };

        EnumChildWindows(hwndParent, callback, IntPtr.Zero);
        return childWindows.ToArray();
    }
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
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
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);

        Windowing.RECT rect = new Windowing.RECT { left = 0, top = 0, right = 800, bottom = 600 };
        IntPtr[] childWindows = Windowing.GetChildWindows(hwnd);

        Windowing.CascadeWindows(hwnd, 0, ref rect, (uint)childWindows.Length, childWindows);
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
    public static readonly IntPtr HWND_TOP = new IntPtr(0);
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
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
        ShowWindow(hWnd, WindowShowStyle.SW_HIDE);
    }
    public static void MaximizeWindow(IntPtr hWnd)
    {
        ShowWindow(hWnd, WindowShowStyle.SW_MAXIMIZE);
    }

    static public async Task<SizeInt32?> SizeWindow()
    {
        var displayList = await DeviceInformation.FindAllAsync
                          (DisplayMonitor.GetDeviceSelector());

        if (!displayList.Any())
            return null;

        var monitorInfo = await DisplayMonitor.FromInterfaceIdAsync(displayList[0].Id);

        var winSize = new SizeInt32();

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

    static public Task DialogWindow(Window window)
    {
        try
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Remove default title bar
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonForegroundColor = Colors.White;
                appWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;

                // Set window size
                var size = new SizeInt32(600, 900);
                appWindow.Resize(size);

                // Remove default window chrome
                var presenter = appWindow.Presenter as OverlappedPresenter;
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
    static public AppWindow GetAppWindow(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(wndId);
    }
}