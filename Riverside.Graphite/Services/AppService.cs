using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.Setup;
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

namespace Riverside.Graphite.Services;

public static class AppService
{
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
				await CloseCancelToken(ref cancellationToken);
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
					_ = WindowsController(cancellationToken).ConfigureAwait(false);
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
					_ = WindowsController(cancellationToken).ConfigureAwait(false);
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
					_ = WindowsController(cancellationToken).ConfigureAwait(false);
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
					_ = WindowsController(cancellationToken).ConfigureAwait(false);
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
			await CloseCancelToken(ref cancellationToken);
			_ = await Task.FromException<CancellationToken>(e);
			throw;
		}

		await Task.FromCanceled(cancellationToken);
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
				string url = protocolArgs.Uri.ToString();
				if (url.StartsWith("http") || url.StartsWith("https"))
				{
					AppArguments.UrlArgument = url;
					ValidateCreatePrivateUser();
					CheckNormal("Private");
				}
				else if (url.StartsWith("firebrowserwinui://"))
				{
					AppArguments.FireBrowserArgument = url;
					ValidateCreatePrivateUser();
					CheckNormal("Private");
				}
				else if (url.StartsWith("firebrowseruser://"))
				{
					AppArguments.FireUser = url;
					string username = ExtractUsernameFromUrl(url);
					if (!string.IsNullOrEmpty(username))
					{
						CheckNormal(username);
						await WindowsController(cancellationToken).ConfigureAwait(false);
						return;
					}
				}
				else if (url.StartsWith("firebrowserincog://"))
				{
					AppArguments.FireBrowserIncog = url;
					ValidateCreatePrivateUser();
					CheckNormal("Private");
				}
				else if (url.Contains(".pdf"))
				{
					AppArguments.FireBrowserPdf = url;
					ValidateCreatePrivateUser();
					CheckNormal("Private");
				}
				await ShowMainWindow(cancellationToken);
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
		if (ActiveWindow is null)
		{
			return;
		}

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
			AppWindowTitleBar titleBar = appWindow.TitleBar;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = btnColor;
			titleBar.ForegroundColor = Colors.LimeGreen;
			titleBar.ButtonBackgroundColor = btnColor;
			titleBar.ButtonInactiveBackgroundColor = btnColor;
			appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
			appWindow.SetIcon("Assets\\AppTiles\\Logo.ico");
		}
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

		ActiveWindow?.Close();
		await ShowMainWindow(cancellationToken);
	}

	private static async Task ShowMainWindow(CancellationToken cancellationToken)
	{
		App.Current.m_window = new MainWindow();
		Windowing.Center(App.Current.m_window);
		IntPtr hWnd = WindowNative.GetWindowHandle(App.Current.m_window);
		_ = Windowing.AnimateWindow(hWnd, 500, Windowing.AW_BLEND | Windowing.AW_VER_POSITIVE | Windowing.AW_HOR_POSITIVE);
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
				_ = (messenger?.Send(new Message_Settings_Actions($"Welcome {AuthService.CurrentUser.Username} to our FireBrowser", EnumMessageStatus.Login)));
			}
		}

		await CloseCancelToken(ref cancellationToken);
	}

	public static string GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
	{
		try
		{
			List<User> users = JsonSerializer.Deserialize<List<User>>(File.ReadAllText(Path.Combine(coreFolderPath, "UsrCore.json")));
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
		// no user return 
		if (userName is null)
		{
			Admin_Create_Account();
			userName = "__Admin__";
		}

		string coreFolderPath = UserDataManager.CoreFolderPath;
		string username = GetUsernameFromCoreFolderPath(coreFolderPath, userName);
		/* store in the datacore project sql file. Going to need to put on cloud, and 
        1. Need function to create file in temp. 
        2. How we push new queries / maybe in cloud for new sql or need function to update 
        3. Migrations are for new and then Update with this new procedure for existing data... 
        Need function after injection, before use logins, and when use authorized */
		string updateSql = Path.Combine(Path.GetTempPath(), "update.sql");



		_ = AuthService.Authenticate(username);

		if (File.Exists(updateSql))
		{
			try
			{
				if (File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
				{
					SettingsActions settingsActions = new(AuthService.CurrentUser.Username);
					string sqlIN = File.ReadAllText(updateSql);
					_ = await settingsActions.SettingsContext.Database.ExecuteSqlRawAsync(sqlIN.Trim());
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
			DatabaseServices dbServer = new();

			try
			{
				_ = await dbServer.DatabaseCreationValidation();
				_ = await dbServer.InsertUserSettings();
				// if we get to here than all is validated and open Browser. 
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
			SizeInt32? desktop = await Windowing.SizeWindow();
			appWindow.MoveAndResize(new RectInt32(desktop.Value.Height / 2, desktop.Value.Width / 2, (int)(desktop?.Width * .75), (int)(desktop?.Height * .75)));
			appWindow.MoveInZOrderAtTop();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.Title = "Settings for: " + AuthService.NewCreatedUser?.Username;
			AppWindowTitleBar titleBar = appWindow.TitleBar;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = btnColor;
			titleBar.ForegroundColor = btnColor;
			titleBar.ButtonBackgroundColor = btnColor;
			titleBar.ButtonInactiveBackgroundColor = btnColor;
			appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
		}

		Windowing.Center(winIncoming);
		appWindow.ShowOnceWithRequestedStartupState();
	}

	public static void Admin_Create_Account()
	{
		Riverside.Graphite.Core.User newUser = new()
		{
			Username = "__Admin__",
		};

		List<Riverside.Graphite.Core.User> users = new() { newUser };
		UserFolderManager.CreateUserFolders(newUser);
		string userFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, newUser.Username);
		if (Directory.Exists(userFolderPath))
		{
			HideDirectory(userFolderPath);
		}

		UserDataManager.SaveUsers(users);
		AuthService.AddUser(newUser);
		_ = AuthService.Authenticate(newUser.Username);
	}

	private static void ValidateCreatePrivateUser()
	{
		string userFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, "Private");

		if (Directory.Exists(userFolderPath))
		{
			return;
		}

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

	public static void Admin_Delete_Account()
	{
		try
		{
			UserDataManager.DeleteUser("__Admin__");
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
		}
	}
}
