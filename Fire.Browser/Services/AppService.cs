using CommunityToolkit.Mvvm.Messaging;
using Fire.Browser.Core;
using Fire.Core.Exceptions;
using Fire.Core.Helpers;
using Fire.Data.Core.Actions;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3.Services.Messages;
using FireBrowserWinUi3.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Graphics;
using WinRT.Interop;

namespace FireBrowserWinUi3.Services;

public static class AppService
{
    public static Window ActiveWindow { get; set; }
    public static Settings AppSettings { get; set; }
    public static CancellationToken CancellationToken { get; set; }
    public static bool IsAppGoingToClose { get; set; }
    public static bool IsAppGoingToOpen { get; set; }
    public static bool IsAppNewUser { get; set; }
    public static bool IsAppUserAuthenicated { get; set; }
    public static IAuthenticationService MsalService { get; set; }
    public static IGraphService GraphService { get; set; }
    public static DispatcherQueue Dispatcher { get; set; }


    public static async Task WindowsController(CancellationToken cancellationToken)
    {
        try
        {
            string changeUsernameFilePath = Path.Combine(Path.GetTempPath(), "changeusername.json");
            string resetFilePath = Path.Combine(Path.GetTempPath(), "Reset.set");
            string backupFilePath = Path.Combine(Path.GetTempPath(), "backup.fireback");
            string restoreFilePath = Path.Combine(Path.GetTempPath(), "restore.fireback");
            string updateSql = Path.Combine(Path.GetTempPath(), "update.sql");

            if (IsAppGoingToClose)
            {
                //throw new ApplicationException("Exiting Application by user");
                await CloseCancelToken(cancellationToken);
                return;
            }

            if (IsAppNewUser)
            {
                CreateNewUsersSettings();
                return;
            }

            // check for restore first. 

            if (File.Exists(restoreFilePath))
            {
                AuthService.Logout();
                ActiveWindow = new RestoreBackUp();
                ActiveWindow.Closed += (s, e) =>
                {
                    WindowsController(cancellationToken).ConfigureAwait(false);
                };
                ActiveWindow.Activate();
                return;
            }

            if (!Directory.Exists(UserDataManager.CoreFolderPath))
            {
                AppSettings = new Settings(true).Self;
                ActiveWindow = new SetupWindow();
                ActiveWindow.Closed += (s, e) => WindowsController(cancellationToken).ConfigureAwait(false);
                await ConfigureSettingsWindow(ActiveWindow);
                return;
            }

            if (File.Exists(changeUsernameFilePath))
            {
                ActiveWindow = new ChangeUsernameCore();
                ActiveWindow.Closed += (s, e) =>
                {
                    AuthService.IsUserNameChanging = false;
                    WindowsController(cancellationToken).ConfigureAwait(false);
                };
                ActiveWindow.Activate();
                return;
            }

            if (File.Exists(backupFilePath))
            {
                AuthService.Logout();
                ActiveWindow = new CreateBackup();
                ActiveWindow.Closed += (s, e) =>
                {
                    WindowsController(cancellationToken).ConfigureAwait(false);
                };
                ActiveWindow.Activate();
                return;
            }


            if (File.Exists(resetFilePath))
            {
                ActiveWindow = new ResetCore();
                ActiveWindow.Closed += (s, e) =>
                {
                    AuthService.IsUserNameChanging = false;
                    WindowsController(cancellationToken).ConfigureAwait(false);
                };
                ActiveWindow.Activate();
                return;
            }

            if (AuthService.CurrentUser == null)
            {
                await HandleProtocolActivation(cancellationToken);
                return;
            }

            if (AuthService.CurrentUser != null && AuthService.IsUserAuthenticated)
            {
                await HandleAuthenticatedUser(cancellationToken);
                return;
            }
        }
        catch (Exception e)
        {
            await CloseCancelToken(cancellationToken);
            await Task.FromException<CancellationToken>(e);
            throw;
        }

        await Task.FromCanceled(cancellationToken);
    }

    public static Task CloseCancelToken(CancellationToken cancellationToken)
    {
        var cancel = new CancellationTokenSource();
        cancellationToken = cancel.Token;
        cancel.Cancel();
        return Task.CompletedTask;
    }
    private static async Task HandleProtocolActivation(CancellationToken cancellationToken)
    {
        try
        {
            var evt = AppInstance.GetActivatedEventArgs();
            if (evt is ProtocolActivatedEventArgs protocolArgs && protocolArgs.Kind == ActivationKind.Protocol)
            {
                string url = protocolArgs.Uri.ToString();
                if (url.StartsWith("http") || url.StartsWith("https"))
                {
                    AppArguments.UrlArgument = url;
                    CheckNormal();
                }
                else if (url.StartsWith("firebrowserwinui://"))
                {
                    AppArguments.FireBrowserArgument = url;
                    CheckNormal();
                }
                else if (url.StartsWith("firebrowseruser://"))
                {
                    AppArguments.FireUser = url;
                    string username = ExtractUsernameFromUrl(url);
                    if (!string.IsNullOrEmpty(username))
                    {
                        CheckNormal(username);
                        await WindowsController(cancellationToken).ConfigureAwait(false);
                    }
                }
                else if (url.StartsWith("firebrowserincog://"))
                {
                    AppArguments.FireBrowserIncog = url;
                    CheckNormal();
                }
                else if (url.Contains(".pdf"))
                {
                    AppArguments.FireBrowserPdf = url;
                    CheckNormal();
                }
            }
            else
            {

                ActiveWindow = new UserCentral();
                ActiveWindow.Closed += (s, e) => WindowsController(cancellationToken).ConfigureAwait(false);
                ConfigureWindowAppearance();
                ActiveWindow.Activate();
                Windowing.Center(ActiveWindow);
            }


        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
            Console.WriteLine($"Activation utilizing Protocol Activation failed..\n {e.Message}");

        }

    }

    private static string ExtractUsernameFromUrl(string url)
    {
        string usernameSegment = url.Replace("firebrowseruser://", "");
        string[] urlParts = usernameSegment.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return urlParts.FirstOrDefault();
    }

    public static void ConfigureWindowAppearance()
    {
        if (ActiveWindow is null) return;

        IntPtr hWnd = WindowNative.GetWindowHandle(ActiveWindow);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(wndId);

        if (appWindow != null)
        {
            appWindow.MoveAndResize(new RectInt32(600, 600, 900, 700));
            appWindow.MoveInZOrderAtTop();
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            // need this for inquires down line for window placement. 
            appWindow.Title = "UserCentral";
            var titleBar = appWindow.TitleBar;
            var btnColor = Colors.Transparent;
            titleBar.BackgroundColor = btnColor;
            titleBar.ForegroundColor = Colors.LimeGreen;
            titleBar.ButtonBackgroundColor = btnColor;
            titleBar.ButtonInactiveBackgroundColor = btnColor;
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            appWindow.SetIcon("logo.ico");
        }
    }

    private static async Task HandleAuthenticatedUser(CancellationToken cancellationToken)
    {

        var userExist = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser?.Username);
        if (!Directory.Exists(userExist))
        {
            UserFolderManager.CreateUserFolders(new User
            {
                Id = Guid.NewGuid(),
                Username = AuthService.CurrentUser.Username,
                IsFirstLaunch = false,
                UserSettings = null
            });
            AppSettings = new Settings(true).Self;
        }

        CheckNormal(AuthService.CurrentUser.Username);

        ActiveWindow?.Close();

        App.Current.m_window = new MainWindow();
        Windowing.Center(App.Current.m_window);
        IntPtr hWnd = WindowNative.GetWindowHandle(App.Current.m_window);
        Windowing.AnimateWindow(hWnd, 500, Windowing.AW_BLEND | Windowing.AW_VER_POSITIVE | Windowing.AW_HOR_POSITIVE);
        App.Current.m_window.Activate();

        App.Current.m_window.AppWindow.MoveInZOrderAtTop();

        List<IntPtr> windows = Windowing.FindWindowsByName(App.Current.m_window?.Title);
        if (windows.Count > 1)
        {
            Windowing.CascadeWindows(windows);
        }


        if (Windowing.IsWindowVisible(hWnd))
        {
            await Task.Delay(1000);
            if (AuthService.IsUserAuthenticated)
            {
                IMessenger messenger = App.GetService<IMessenger>();
                messenger?.Send(new Message_Settings_Actions($"Welcome {AuthService.CurrentUser.Username} to our FireBrowser", EnumMessageStatus.Login));
            }
        }

        var cancel = new CancellationTokenSource();
        CancellationToken = cancellationToken = cancel.Token;
        cancel.Cancel();
    }

    public static string GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
    {
        try
        {
            var users = JsonSerializer.Deserialize<List<User>>(File.ReadAllText(Path.Combine(coreFolderPath, "UsrCore.json")));
            return users?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.Username) && (userName == null || u.Username.Equals(userName, StringComparison.CurrentCultureIgnoreCase)))?.Username;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading UsrCore.json: {ex.Message}");
        }

        return null;
    }

    private static async void CheckNormal(string userName = null)
    {
        string coreFolderPath = UserDataManager.CoreFolderPath;
        string username = GetUsernameFromCoreFolderPath(coreFolderPath, userName);
        /* store in the datacore project sql file. Going to need to put on cloud, and 
        1. Need function to create file in temp. 
        2. How we push new queries / maybe in cloud for new sql or need function to update 
        3. Migrations are for new and then Update with this new procedure for existing data... 
        Need function after injection, before use logins, and when use authorized */
        string updateSql = Path.Combine(Path.GetTempPath(), "update.sql");

        if (userName is null) return;

        AuthService.Authenticate(username);

        if (File.Exists(updateSql))
        {
            try
            {
                if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
                {
                    SettingsActions settingsActions = new SettingsActions(AuthService.CurrentUser.Username);
                    var sqlIN = File.ReadAllText(updateSql);
                    await settingsActions.SettingsContext.Database.ExecuteSqlRawAsync(sqlIN.Trim());
                    File.Delete(updateSql);
                }

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                IsAppGoingToClose = true;
                throw;
            }
        }

        if (AuthService.IsUserAuthenticated)
        {

            DatabaseServices dbServer = new DatabaseServices();

            try
            {

                await dbServer.DatabaseCreationValidation();
                await dbServer.InsertUserSettings();
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                Console.WriteLine($"Creating Settings for user already exists\n {ex.Message}");
            }
        }
    }

    public static async void CreateNewUsersSettings()
    {
        ActiveWindow = new UserSettings();
        ActiveWindow.Closed += async (s, e) =>
        {
            try
            {
                if (AuthService.NewCreatedUser is not null)
                {
                    var settingsActions = new SettingsActions(AuthService.NewCreatedUser?.Username);
                    var settingsPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser?.Username, "Settings", "Settings.db");

                    if (!File.Exists(settingsPath))
                    {
                        await settingsActions.SettingsContext.Database.MigrateAsync();
                    }

                    if (File.Exists(settingsPath))
                    {
                        await settingsActions.SettingsContext.Database.CanConnectAsync();
                    }

                    if (await settingsActions.GetSettingsAsync() is null)
                    {
                        await settingsActions.UpdateSettingsAsync(AppSettings);
                    }

                }

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
            }
            //finally
            //{
            //    AuthService.NewCreatedUser = null;
            //}
        };

        await ConfigureSettingsWindow(ActiveWindow);

    }

    public static async Task ConfigureSettingsWindow(Window winIncoming)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(winIncoming);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(wndId);

        if (appWindow != null)
        {
            SizeInt32? desktop = await Windowing.SizeWindow();
            appWindow.MoveAndResize(new RectInt32(desktop.Value.Height / 2, desktop.Value.Width / 2, (int)(desktop?.Width * .75), (int)(desktop?.Height * .75)));
            appWindow.MoveInZOrderAtTop();
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.Title = "Settings for: " + AuthService.NewCreatedUser?.Username;
            var titleBar = appWindow.TitleBar;
            var btnColor = Colors.Transparent;
            titleBar.BackgroundColor = btnColor;
            titleBar.ForegroundColor = btnColor;
            titleBar.ButtonBackgroundColor = btnColor;
            titleBar.ButtonInactiveBackgroundColor = btnColor;
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }

        Windowing.Center(winIncoming);
        appWindow.ShowOnceWithRequestedStartupState();
    }
}
