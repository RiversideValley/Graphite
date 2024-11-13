using CommunityToolkit.Mvvm.Messaging;
using Fire.Browser.Core;
using Fire.Core.Exceptions;
using FireBrowserWinUi3.Services;
using FireBrowserWinUi3.Services.ViewModels;
using FireBrowserWinUi3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace FireBrowserWinUi3;
public partial class App : Application
{
	private readonly string changeUsernameFilePath = Path.Combine(Path.GetTempPath(), "changeusername.json");
	public static new App Current => (App)Application.Current;

	public NotificationManager NotificationManager { get; set; }

	private string AzureStorage { get; } = "DefaultEndpointsProtocol=https;AccountName=strorelearn;AccountKey=0pt8CYqrqXUluQE3/60q8wobkmYznb9ovHIzztGVOzNxlSa+U8NlY74uwfggd5DfTmGORBLtXpeKEvDYh2ynfQ==;EndpointSuffix=core.windows.net";

	#region DependencyInjection

	public IServiceProvider Services { get; set; }

	public static T GetService<T>() where T : class
	{
		if (App.Current is not App || App.Current.Services is null)
		{
			throw new NullReferenceException("Application or Services are not properly initialized.");
		}

		return App.Current.Services.GetService(typeof(T)) is not T service
			? throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.")
			: service;
	}
	public IServiceProvider ConfigureServices()
	{
		ServiceCollection services = new();

		_ = services.AddSingleton<WeakReferenceMessenger>();
		_ = services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider =>
			provider.GetRequiredService<WeakReferenceMessenger>());
		_ = services.AddSingleton<DownloadService>();
		_ = services.AddSingleton<MsalAuthService>();
		_ = services.AddSingleton<GraphService>();
		_ = services.AddTransient<DownloadsViewModel>();
		_ = services.AddTransient<HomeViewModel>();
		_ = services.AddTransient<SettingsService>();
		_ = services.AddTransient<MainWindowViewModel>();
		_ = services.AddTransient<UploadBackupViewModel>();

		return services.BuildServiceProvider();
	}

	#endregion

	public App()
	{

		AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
		InitializeComponent();
		UnhandledException += Current_UnhandledException;

		Environment.SetEnvironmentVariable("WEBVIEW2_USE_VISUAL_HOSTING_FOR_OWNED_WINDOWS", "1");

		Environment.SetEnvironmentVariable("WEBVIEW2_CHANNEL_SEARCH_KIND", "1");
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--window-size=0,0 --window-position=40000,40000");
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-extensions");
		Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] = AzureStorage;
	}
	public static string GetFullPathToExe()
	{
		var path = AppDomain.CurrentDomain.BaseDirectory;
		var pos = path.LastIndexOf("\\");
		return path.Substring(0, pos);
	}

	public static string GetFullPathToAsset(string assetName)
	{
		return GetFullPathToExe() + "\\Assets\\" + assetName;
	}
	void OnProcessExit(object sender, EventArgs e)
	{
		NotificationManager.Unregister();
	}
	private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		if (!AppService.IsAppGoingToClose)
		{
			Fire.Core.Exceptions.ExceptionLogger.LogException(e.Exception);
		}
	}

	public static string GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
	{
		try
		{
			List<Fire.Browser.Core.User> users = JsonSerializer.Deserialize<List<Fire.Browser.Core.User>>(File.ReadAllText(Path.Combine(coreFolderPath, "UsrCore.json")));

			return users?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.Username) && (userName == null || u.Username.Equals(userName, StringComparison.CurrentCultureIgnoreCase)))?.Username;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error reading UsrCore.json: {ex.Message}");
		}

		return null;
	}

	protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
	{


		App.Current.Services = App.Current.ConfigureServices();
		NotificationManager = new NotificationManager();
		NotificationManager.Init();

		var InstanceCreationToken = AppService.CancellationToken = CancellationToken.None;

		try
		{
			AppService.MsalService = App.GetService<MsalAuthService>();
			AppService.GraphService = App.GetService<GraphService>();
			NotificationManager = new NotificationManager();
			NotificationManager.Init();
		}
		catch (Exception e)
		{

			ExceptionLogger.LogException(e);
		}

		try
		{
			await AppService.WindowsController(AppService.CancellationToken);

			while (!AppService.CancellationToken.IsCancellationRequested)
			{
				await Task.Delay(2400);
			}

			if (AppService.IsAppGoingToClose == true)
			{
				base.Exit();
			}
			else
			{
				base.OnLaunched(args);
			}
		}
		catch (Exception ex)
		{
			await AppService.CloseCancelToken(ref InstanceCreationToken);
			ExceptionLogger.LogException(ex);
		}

		var currentInstance = AppInstance.GetCurrent();
		if (currentInstance.IsCurrent)
		{
			// AppInstance.GetActivatedEventArgs will report the correct ActivationKind,
			// even in WinUI's OnLaunched.
			AppActivationArguments activationArgs = currentInstance.GetActivatedEventArgs();
			if (activationArgs != null)
			{
				ExtendedActivationKind extendedKind = activationArgs.Kind;
				if (extendedKind == ExtendedActivationKind.AppNotification)
				{
					var notificationActivatedEventArgs = (AppNotificationActivatedEventArgs)activationArgs.Data;
					NotificationManager.ProcessLaunchActivationArgs(notificationActivatedEventArgs);
				}
			}
		}
	}

	public Window m_window;
}