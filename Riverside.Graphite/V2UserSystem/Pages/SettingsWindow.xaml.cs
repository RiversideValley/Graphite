using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
using Graphite.UserSys;
using Riverside.Graphite.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Graphite.Pages
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsWindow : Window
    {
		private AppWindow appWindow;
		private UserV2 USR;

		public SettingsWindow(UserV2 user)
        {
            this.InitializeComponent();
			USR = user;
			TitleTop();
		}

		public void TitleTop()
		{
			nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			appWindow = AppWindow.GetFromWindowId(windowId);
			appWindow.SetIcon("Logo.ico");
			AppWindow.Title = $"Browser Settings - {USR.Username}";

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
		
		
	}
}
