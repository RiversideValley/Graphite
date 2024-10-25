using FireBrowserWinUi3.Services;
using FireBrowserWinUi3.Services.Models;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3MultiCore;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace FireBrowserWinUi3
{

    public sealed partial class UserCentral : Window
    {
        private readonly AppWindow appWindow;
        public static UserCentral Instance { get; private set; }
        public UC_Viewmodel ViewModel { get; set; }
        public static bool IsOpen { get; private set; }
        public bool _isDataLoaded { get; set; }

        public UserCentral()
        {
            InitializeComponent();
            ViewModel = new UC_Viewmodel { ParentWindow = this, ParentGrid = GridUserCentral };
            Instance = this;
            Activated += async (_, _) =>
            {
                if (!_isDataLoaded)
                {
                    await LoadDataGlobally();
                    _isDataLoaded = true;
                }
            };

            // Get the AppWindow for this window
            Windowing.DialogWindow(this).ConfigureAwait(false);
        }

        public async Task LoadDataGlobally()
        {
            if (!_isDataLoaded)
            {
                string coreFolderPath = UserDataManager.CoreFolderPath;
                ViewModel.Users = await GetUsernameFromCoreFolderPath(coreFolderPath);
                UserListView.ItemsSource = ViewModel.Users;
                ViewModel.RaisePropertyChanges(nameof(ViewModel.Users));
                _isDataLoaded = true;
            }
        }

        private async Task<List<UserExtend>> GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
        {
            try
            {
                string usrCoreFilePath = Path.Combine(coreFolderPath, "UsrCore.json");
                if (File.Exists(usrCoreFilePath))
                {
                    string jsonContent = await File.ReadAllTextAsync(usrCoreFilePath);
                    var users = JsonSerializer.Deserialize<List<User>>(jsonContent);

                    return users?.FindAll(user => !user.Username.Contains("Private"))?.ConvertAll(user => new UserExtend(user));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading UsrCore.json: {ex.Message}");
            }
            return new List<UserExtend>();
        }

        private void UserListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && UserListView.SelectedItem is UserExtend selectedUser)
            {
                ViewModel.User = selectedUser;
                ViewModel.RaisePropertyChanges(nameof(ViewModel.User));

                if (AuthService.users.Count == 0)
                    AuthService.users = AuthService.LoadUsersFromJson();

                AuthService.Authenticate(selectedUser.FireUser.Username);
                // close active window if not Usercentral, and then assign it as usercentral and close to give -> windowscontroller notification of close usercentral 
                AppService.ActiveWindow?.Close();
                AppService.ActiveWindow = this;
                AppService.ActiveWindow?.Close();
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var usr = new AddUserWindow();
            usr.Closed +=  (s, e) =>
            {
                AppService.ActiveWindow = this;
            };
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            if (hWnd != IntPtr.Zero)
            {
                Windowing.SetWindowPos(hWnd, Windowing.HWND_BOTTOM, 0, 0, 0, 0, Windowing.SWP_NOSIZE);
            }
            await AppService.ConfigureSettingsWindow(usr);
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (appWindow != null)
            {
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.Minimize();
                }
            }
        }


    }
}