using FireBrowserWinUi3.Pages.SettingsPages;
using FireBrowserWinUi3.Services;
using Fire.Core.Helpers;
using Fire.Browser.Core;
using Fire.Browser.Core.Helper;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace FireBrowserWinUi3
{
    public sealed partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            this.InitializeComponent();
            ConfigureWindowAppearance(this);
            this.Activated += (s, e) => { Userbox.Focus(FocusState.Programmatic); };
            this.Closed += async (s, e) =>
            {

                await Task.Delay(420);

                IntPtr ucHwnd = Windowing.FindWindow(null, nameof(UserCentral));
                if (ucHwnd != IntPtr.Zero)
                {
                    Windowing.Center(ucHwnd);
                    Windowing.UpdateWindow(ucHwnd);
                }
                else
                {
                    Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                }
            };
        }

        private static void ConfigureWindowAppearance(Window wdn)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(wdn);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(wndId);
            if (appWindow != null)
            {
                // Set the maximum size to 430 width and 612 height
                appWindow.SetPresenter(AppWindowPresenterKind.Default);
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsResizable = true;
                    presenter.IsMaximizable = false;
                    presenter.IsModal = false;
                }
                appWindow.MoveAndResize(new RectInt32(600, 600, 430, 612));
                appWindow.SetPresenter(AppWindowPresenterKind.Default);

                appWindow.MoveInZOrderAtTop();
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.Title = "Create New User";
                appWindow.SetIcon("LogoSetup.ico");
                var titleBar = appWindow.TitleBar;
                var btnColor = Colors.Transparent;
                titleBar.BackgroundColor = btnColor;
                titleBar.ForegroundColor = btnColor;
                titleBar.ButtonBackgroundColor = btnColor;
                titleBar.BackgroundColor = btnColor;
                titleBar.ButtonInactiveBackgroundColor = btnColor;
            }
        }

        string iImage = "";
        public async Task CopyImageAsync(string iImage, string destinationFolderPath)
        {
            ImageHelper imgLoader = new ImageHelper();
            imgLoader.ImageName = iImage;
            imgLoader.LoadImage($"{iImage}");

            StorageFolder destinationFolder = await StorageFolder.GetFolderFromPathAsync(destinationFolderPath);

            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Fire.Browser.Core/Assets/{iImage}"));
            StorageFile destinationFile = await imageFile.CopyAsync(destinationFolder, "profile_image.jpg", NameCollisionOption.ReplaceExisting);

            Console.WriteLine("Image copied successfully!");
        }

        private void ProfileImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileImage.SelectedItem != null)
            {
                string userImageName = ProfileImage.SelectedItem.ToString() + ".png";

                iImage = userImageName;
                ImageHelper imgLoader = new ImageHelper();
                var userProfilePicture = imgLoader.LoadImage(userImageName);
                Pimg.ProfilePicture = userProfilePicture;
            }
        }

        private void Userbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsrBox.Text = Userbox.Text;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string enteredUsername = Userbox.Text;

            if (string.IsNullOrWhiteSpace(enteredUsername)) return;

            User newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = enteredUsername,
                IsFirstLaunch = false,
                UserSettings = null
            };

            List<Fire.Browser.Core.User> users = new List<Fire.Browser.Core.User>();
            users.Add(newUser);

            AuthService.AddUser(newUser);

            UserFolderManager.CreateUserFolders(newUser);

            string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, Userbox.Text.ToString());

            await CopyImageAsync(iImage.ToString(), destinationFolderPath);

            AuthService.NewCreatedUser = newUser;

            if (SettingsHome.Instance is not null)
                SettingsHome.Instance?.LoadUsernames();
            if (UserCentral.Instance is not null)
            {
                await UserCentral.Instance?.LoadDataGlobally();
            }


            AppService.CreateNewUsersSettings();
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            if (hWnd != IntPtr.Zero)
            {
                Windowing.HideWindow(hWnd);
            }


        }

        private void Button_Click_Close(object sender, RoutedEventArgs e)
        {

            IntPtr hUser = Windowing.FindWindow(null, nameof(UserCentral));
            if (hUser != IntPtr.Zero)
            {
                Windowing.Center(hUser);
            }

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            if (hWnd != IntPtr.Zero)
            {
                Windowing.HideWindow(hWnd);
            }

        }
    }
}