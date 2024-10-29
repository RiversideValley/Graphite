using CommunityToolkit.Mvvm.DependencyInjection;
using FireBrowserWinUi3.Services.ViewModels;
using FireBrowserWinUi3Core.Helpers;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            this.InitializeComponent();

            ShowWindowSlide().ConfigureAwait(false);
        }


        private DispatcherTimer timer;
        private int bounceCount = 0;
        private int screenWidth, screenHeight, windowWidth, windowHeight;

        private async Task ShowWindowSlide()
        {
            var desktop = await Windowing.SizeWindow();
            // Get screen dimensions
            screenWidth = (int)desktop.Value.Width;
            screenHeight = (int)desktop.Value.Height;

            // Get window dimensions
            windowWidth = (int)this.AppWindow.Size.Width;
            windowHeight = (int)this.AppWindow.Size.Height;

            var windowHandle = WindowNative.GetWindowHandle(this);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(20); // Adjust the interval for smoother animation
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
            Windowing.SetWindowPos(hwnd, IntPtr.Zero, centerX, newY, 0, 0, Windowing.SWP_NOZORDER | Windowing.SWP_NOSIZE);

            // Stop the animation after a while
            if (bounceCount > 30)
            {
                timer.Stop();
                // Ensure window ends at the center position
                Windowing.SetWindowPos(hwnd, IntPtr.Zero, centerX, endY, 0, 0, Windowing.SWP_NOZORDER | Windowing.SWP_NOSIZE);

            }
        }
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(ViewModel.FileSelected.BlobUrl);
            Clipboard.SetContent(dataPackage);
        }
    }
}
