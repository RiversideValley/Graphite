using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace FireBrowserWinUi3.Controls;
public sealed partial class ResetCore : Window
{
    private AppWindow appWindow;
    private AppWindowTitleBar titleBar;

    public ResetCore()
    {
        this.InitializeComponent();
        InitializeWindow();
        Thread.Sleep(1000);
        HandleDeletion().ConfigureAwait(false);
    }

    private void InitializeWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.MoveAndResize(new RectInt32(500, 500, 850, 500));
        FireBrowserWinUi3Core.Helpers.Windowing.Center(this);
        appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
        appWindow.MoveInZOrderAtTop();
        appWindow.ShowOnceWithRequestedStartupState();
        appWindow.SetIcon("logo.ico");

        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            throw new Exception("Unsupported OS version.");
        }
        else
        {
            titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            var btnColor = Colors.Transparent;
            titleBar.BackgroundColor = btnColor;
            titleBar.ButtonBackgroundColor = btnColor;
            titleBar.InactiveBackgroundColor = btnColor;
            titleBar.ButtonInactiveBackgroundColor = btnColor;
        }
    }

    private async Task HandleDeletion()
    {
        string tempPath = Path.GetTempPath();
        string patchFilePath = Path.Combine(tempPath, "Reset.set");

        try
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderToDelete = Path.Combine(documentsPath, "FireBrowserUserCore");
            Directory.Delete(folderToDelete, true);
            File.Delete(patchFilePath);
        }
        catch (Exception)
        {
            // Swallow exceptions to avoid displaying errors
        }
        Thread.Sleep(2500);
        await RestartApplication();
    }

    private  Task RestartApplication()
    {
        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        return Task.CompletedTask; 
    }
}
