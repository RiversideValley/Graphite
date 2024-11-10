using Fire.Core.Helpers;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using WinRT.Interop;

namespace FireBrowserWinUi3;

public sealed partial class SetupFinish : Page
{
    public SetupFinish()
    {
        this.InitializeComponent();
        this.Loaded += SetupFinish_Loaded;
    }

    private async void SetupFinish_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(2400);

        if (App.Current.m_window is not null)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(App.Current.m_window);
            if (hWnd == IntPtr.Zero)
            {
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
            }
            else
            {
                if (Windowing.IsWindow(hWnd))
                    Windowing.ShowWindow(hWnd, Windowing.WindowShowStyle.SW_RESTORE);

                AppService.ActiveWindow?.Close();
            }
        }
        else
        {
            AppService.ActiveWindow?.Close();

            IntPtr ucHwnd = Windowing.FindWindow(null, nameof(UserCentral));
            if (ucHwnd != IntPtr.Zero)
            {
                AppService.ActiveWindow = UserCentral.Instance;
                Windowing.Center(ucHwnd);
            }
            else
            {
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
            }
        }
    }
}