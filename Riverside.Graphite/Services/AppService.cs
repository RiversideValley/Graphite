using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Graphite;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using NuGet.Protocol;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Update;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.Services.ViewModels;
using Riverside.Graphite.Services.WindowsHandler;
using Riverside.Graphite.Services.WindowsHandler.Contracts;
using Riverside.Graphite.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Graphics;
using WinRT.Interop;

namespace Riverside.Graphite.Services;

public static class AppService
{
	private static readonly object _lock = new();
	private static IWindowHandler _appServiceWindowHandler;
	public static IWindowHandler AppServiceWindowHandler
	{
		get
		{
			lock (_lock)
			{
				return _appServiceWindowHandler;
			}
		}
		set
		{
			lock (_lock)
			{
				_appServiceWindowHandler = value;
			}
		}
	}
	public static Window ActiveWindow { get; set; }
	public static HashSet<Window> FireWindows { get; set; }
	public static Settings AppSettings { get; set; }
	public static CancellationToken CancellationToken { get; set; }
	public static bool IsAppGoingToClose { get; set; }
	public static bool IsAppGoingToOpen { get; set; }
	public static bool IsAppNewUser { get; set; }
	public static bool IsAppUserAuthenicated { get; set; }
	public static IAuthenticationService MsalService { get; set; }
	public static IGraphService GraphService { get; set; }
	public static DispatcherQueue Dispatcher { get; set; }
	public static AppServiceViewModel AppServiceViewModel { get; set; } = new();

    public static async Task WindowsController(CancellationToken cancellationToken)
    {
        try
        {
            var OperationsPending = new Dictionary<string, Action>
            {
                { "changeusername.json", () => OpenWindow(new ChangeUsernameCore(), cancellationToken) },
                { "Reset.set", () => OpenWindow(new ResetCore(), cancellationToken) },
                { "backup.fireback", () => OpenWindow(new CreateBackup(), cancellationToken) },
                { "restore.fireback", () => OpenWindow(new RestoreBackUp(), cancellationToken) }
            };

            if (IsAppGoingToClose)
            {
                await CloseCancelToken(ref cancellationToken);
                return;
            }

            if (IsAppNewUser)
            {
                CreateNewUsersSettings();
                return;
            }

            foreach (var ops in OperationsPending)
            {
                if (File.Exists(Path.Combine(Path.GetTempPath(), ops.Key)))
                {
                    ops.Value.Invoke();
                    return;
                }
            }

            if (!Directory.Exists(UserDataManager.CoreFolderPath))
            {
                AppSettings = new Settings(true).Self;
                ActiveWindow = new SetupWindow();
                ActiveWindow.Closed += (s, e) => WindowsController(cancellationToken).ConfigureAwait(false);
                await ConfigureSettingsWindow(ActiveWindow);
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
            await CloseCancelToken(ref cancellationToken);
            _ = await Task.FromException<CancellationToken>(e);
            throw;
        }

        await Task.FromCanceled(cancellationToken);
    }

	private static void OpenWindow(Window window, CancellationToken cancellationToken)
    {
        AuthService.Logout();
        ActiveWindow = window;
        ActiveWindow.Closed += (s, e) => WindowsController(cancellationToken).ConfigureAwait(false);
        ActiveWindow.Activate();
    }
	public static Task CloseCancelToken(ref CancellationToken cancellationToken)
	{
		// need to assign reference token in order to cancel !
		CancellationTokenSource cancel = new();
		cancel.Cancel();
		CancellationToken = cancellationToken = cancel.Token;
		return Task.CompletedTask;
	}
	private static async Task HandleProtocolActivation(CancellationToken cancellationToken)
	{
		try
		{
			IActivatedEventArgs evt = AppInstance.GetActivatedEventArgs();
			if (evt is ProtocolActivatedEventArgs protocolArgs && protocolArgs.Kind == ActivationKind.Protocol)
			{
                string url = protocolArgs.Uri.Scheme.ToString();

                var urlActions = new Dictionary<string, Action>
                {
                    { "http", () => { AppArguments.UrlArgument = url; ValidateCreatePrivateUser(); CheckNormal("Private"); } },
                    { "https", () => { AppArguments.UrlArgument = url; ValidateCreatePrivateUser(); CheckNormal("Private"); } },
                    { "firebrowserwinui", () => { AppArguments.FireBrowserArgument = url; ValidateCreatePrivateUser(); CheckNormal("Private"); } },
                    { "firebrowseruser", async () => {
						AppArguments.FireUser = protocolArgs.Uri.AbsoluteUri;
                        string username = ExtractUsernameFromUrl(protocolArgs.Uri.AbsoluteUri);
                        if (!string.IsNullOrEmpty(username))
                        {
                            CheckNormal(username);
                            await WindowsController(cancellationToken).ConfigureAwait(false);
                            return;
                        }
                    }},
                    { "firebrowserincog", () => { AppArguments.FireBrowserIncog = url; ValidateCreatePrivateUser(); CheckNormal("Private"); } },
                    { ".pdf", () => { AppArguments.FireBrowserPdf = url; ValidateCreatePrivateUser(); CheckNormal("Private"); } }
                };

                foreach (var action in urlActions)
                {
                    if (url.StartsWith(action.Key) || url.Contains(action.Key))
                    {
                        action.Value.Invoke();
                        return;
                    }
                }
				await ShowMainWindow(cancellationToken);
			}
			else
			{
				ActiveWindow = new UserDashBoard();
				await ConfigureSettingsWindow(ActiveWindow).ConfigureAwait(false);
				
				ActiveWindow.Closed += async (s, e) =>
				{
					if (ActiveWindow is UserDashBoard dash)
					{

						if (dash.AuthUser is not null)
						{
							AuthService.AddUser(new User
							{
								Email = dash.AuthUser.Email,
								Id = Guid.Parse(dash.AuthUser.SessionId),
								IsFirstLaunch = dash.AuthUser.IsFirstLaunch,
								Password = dash.AuthUser.HasPassword.ToString(),
								Username = dash.AuthUser.Username,
								WindowsUserName = dash.AuthUser.WindowsUserName
							});
							AuthService.Authenticate(dash.AuthUser.Username);	
						}
						else {
							if (dash.CancellationToken.IsCancellationRequested)
							{
								IsAppGoingToClose = true;
							}
						}

					}
					
					await WindowsController(cancellationToken).ConfigureAwait(false);
				};

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

	
	private static async Task HandleAuthenticatedUser(CancellationToken cancellationToken)
	{
		string userExist = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser?.Username);
		
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

		if (ActiveWindow is not null)
		{
			if (Windowing.IsWindow(WindowNative.GetWindowHandle(ActiveWindow)))
				ActiveWindow?.Close();

		}

		await ShowMainWindow(cancellationToken);
	}

	private static async Task ShowMainWindow(CancellationToken cancellationToken)
	{
		 
		App.Current.m_window = new MainWindow();
		await App.Current.InitializeWindowHandler(App.Current.m_window);
		
		if (App.Current.WindowHandler is IWindowHandler WindowHandler)
		{
			AppService.AppServiceWindowHandler = WindowHandler; 
			WindowHandler.Initialize(App.Current.m_window);
			WindowHandler.SetWindowBackdrop(BackdropType.MicaAlt);
			WindowHandler.SetIcon("ms-appx:///Assets/Logo.ico");
			//SizeInt32? desktop = await Windowing.SizeWindow();
			//WindowHandler.SetWindowSize((int)(desktop?.Width * .75), (int)(desktop?.Height * .75));
			////WindowHandler.CenterOnScreen();
			WindowHandler.RestoreWindowPosition();
			WindowHandler.SetTitle("Graphite Browser");
			_ = Windowing.AnimateWindow(WindowHandler?.Hwnd != default ? WindowHandler.Hwnd : WindowNative.GetWindowHandle(App.Current.m_window), 500, Windowing.AW_BLEND | Windowing.AW_VER_POSITIVE | Windowing.AW_HOR_POSITIVE);
			App.Current.m_window.AppWindow.MoveInZOrderAtTop();
			WindowHandler.AppWindow?.ShowOnceWithRequestedStartupState();
		}

		List<IntPtr> windows = Windowing.FindWindowsByName(App.Current.m_window?.Title);

		if (windows.Count > 1)
		{
			Windowing.CascadeWindows(windows);
		}

		if (Windowing.IsWindowVisible(WindowNative.GetWindowHandle(App.Current.m_window!)))
		{
			await Task.Delay(1000);
			if (AuthService.IsUserAuthenticated)
			{
				IMessenger messenger = App.GetService<IMessenger>();
				_ = (messenger?.Send(new Message_Settings_Actions($"Welcome {AuthService.CurrentUser.Username} to our FireBrowser", EnumMessageStatus.Login)));
			}
		}

		await CloseCancelToken(ref cancellationToken);
	}

	public static string UserExistDatabase(string userName = null)
	{
		try
		{
			return  UserManager.GetAllUsersAsync().Result.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.Username) && (userName == null || u.Username.Equals(userName, StringComparison.CurrentCultureIgnoreCase)))?.Username;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error reading UsrCore.json: {ex.Message}");
		}

		return null;
	}

	private static async void CheckNormal(string userName = null)
	{
		if (userName is null)
			return;	

		string coreFolderPath = UserDataManager.CoreFolderPath;
		string username = UserExistDatabase(userName);
	
		AuthService.Authenticate(username);

		if (!AuthService.IsUserAuthenticated) return; 

		try
		{
		DatabaseServices dbServer = new();

		// DATABASE EXISTS && CONNECTS AND MIGRATIONS. 
		_ = await dbServer.DatabaseCreationValidation();

			_ = await dbServer.InsertUserSettings(); // new user add default from class

			HistoryActions historyActions = new(AuthService.CurrentUser.Username);

			if (await historyActions.HistoryContext.Database.CanConnectAsync()) {

				if (historyActions.HistoryContext.CollectionNames.Count() == 0)
					dbServer.CreateCollections(); 
			}
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Creating Settings for user already exists\n {ex.Message}");
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
					SettingsActions settingsActions = new(AuthService.NewCreatedUser?.Username);
					string settingsPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser?.Username, "Settings", "Settings.db");

					if (!File.Exists(settingsPath))
					{
						await settingsActions.SettingsContext.Database.MigrateAsync();
					}

					if (File.Exists(settingsPath))
					{
						_ = await settingsActions.SettingsContext.Database.CanConnectAsync();
					}

					if (await settingsActions.GetSettingsAsync() is null)
					{
						_ = await settingsActions.UpdateSettingsAsync(AppSettings);
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
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

			SizeInt32? desktop = await Windowing.SizeWindow();
			appWindow.MoveAndResize(new RectInt32(desktop.Value.Height / 2, desktop.Value.Width / 2, (int)(desktop?.Width * .66), (int)(desktop?.Height * .66)));
			appWindow.SetIcon("ms-appx:///Assets/AppTiles/Logo.ico");
			appWindow.MoveInZOrderAtTop();
			
			appWindow.Title = "Settings for: " + AuthService.CurrentUser?.Username ?? AuthService.NewCreatedUser?.Username;
			AppWindowTitleBar titleBar = appWindow.TitleBar;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = btnColor;
			titleBar.ForegroundColor = Colors.WhiteSmoke;
			titleBar.ButtonBackgroundColor = btnColor;
			titleBar.ButtonInactiveBackgroundColor = btnColor;
			appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
		}

		Windowing.Center(winIncoming);
		appWindow.ShowOnceWithRequestedStartupState();
	}

	private static void ValidateCreatePrivateUser()
	{
		string userFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, "Private");

		// folders might exist but user of "Private" doesn't validate & create
		if (Directory.Exists(userFolderPath))
		{
			string username = UserExistDatabase(userFolderPath);
			if (username is not null)
				return;
		}

		if (AuthService.UserExists("Private") is null)
		{
			User newUser = new()
			{
				Id = Guid.NewGuid(),
				Username = "Private",
				IsFirstLaunch = true,
				UserSettings = null
			};

			AuthService.AddUser(newUser);
			UserFolderManager.CreateUserFolders(newUser);
		}


	}

	private static void HideDirectory(string directoryPath)
	{
		if (Directory.Exists(directoryPath))
		{
			FileAttributes attributes = File.GetAttributes(directoryPath);
			if ((attributes & FileAttributes.Hidden) == 0)
			{
				attributes |= FileAttributes.Hidden;
				File.SetAttributes(directoryPath, attributes);
			}
		}
		else
		{
			throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
		}
	}
	
}
