using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services;
using Riverside.Graphite.Services.ViewModels;
using Riverside.Graphite.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Riverside.Graphite;
public partial class App : Application
{
	public static new App Current => (App)Application.Current;

	private Process _webAppProcess;
	public NotificationManager NotificationManager { get; set; }

	private string AzureStorage { get; } = "DefaultEndpointsProtocol=https;AccountName=strorelearn;AccountKey=0pt8CYqrqXUluQE3/60q8wobkmYznb9ovHIzztGVOzNxlSa+U8NlY74uwfggd5DfTmGORBLtXpeKEvDYh2ynfQ==;EndpointSuffix=core.windows.net";

	#region DependencyInjection

	public IServiceProvider Services { get; set; }

	public static T GetService<T>() where T : class
	{
		return App.Current is null || App.Current.Services is null
			? throw new NullReferenceException("Application or Services are not properly initialized.")
			: App.Current.Services.GetService(typeof(T)) is not T service
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
		_ = services.AddTransient<AdBlockerWrapper>();
		_ = services.AddTransient<DownloadsViewModel>();
		_ = services.AddTransient<HomeViewModel>();
		_ = services.AddTransient<SettingsService>();
		_ = services.AddTransient<MainWindowViewModel>();
		_ = services.AddTransient<UploadBackupViewModel>();
		//_ = services.AddSignalR(options =>
		//{
		//	options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(120000);
		//	options.KeepAliveInterval = TimeSpan.FromMilliseconds(840000);
		//}).AddAzureSignalR(conn =>
		//{
		//	conn.ConnectionString = "Endpoint=https://energy.service.signalr.net;AccessKey=HEJiDy8KLt4s8ixioCIWTCczqHAVzzZ9WXpJUQzzpyc=;Version=1.0;";
		//	conn.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Preferred;
		//	conn.ClaimsProvider = context =>
		//	[
		//		new Claim(ClaimTypes.NameIdentifier, context?.Request?.Query["username"]!)
		//	];
		//});
		//_ = services.AddSingleton<HubService>();

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

		AppService.FireWindows = new HashSet<Window>();



		try
		{
			//kill any hanging instance of channels, especially in debugging..
			KillProcessByName("dotnet");

			Task.Run(() => StartChannels());
		}
		catch (Exception e)
		{
			ExceptionLogger.LogException(e);
			throw;
		}

	}

	static void KillProcessByName(string processName)
	{
		try
		{
			var processes = Process.GetProcessesByName(processName);

			if (processes.Length == 0)
			{
				Console.WriteLine($"No processes found with the name: {processName}");
				return;
			}

			foreach (var process in processes)
			{
				Console.WriteLine($"Killing process: {process.ProcessName} (ID: {process.Id})");
				process.Kill();
				process.WaitForExit(); // Optional: wait for the process to exit
				Console.WriteLine($"Process {process.ProcessName} (ID: {process.Id}) terminated.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
		}
	}



	private void StartChannels()
	{
		string publishDirectory = Get_Appx_AssemblyDirectory(typeof(Riverside.Graphite.Channels.Program).Assembly);
		string webAppPath = Path.Combine(publishDirectory, "RiverSide.Graphite.Channels.dll");
		try
		{
			if (!File.Exists(webAppPath))
				return;

			var startInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				FileName = "dotnet.exe",
				Arguments = webAppPath,
				WorkingDirectory = publishDirectory
			};

			_webAppProcess = Process.Start(startInfo);
			_webAppProcess.WaitForExit();
		}
		catch (Exception e)
		{
			ExceptionLogger.LogException(e);
			throw;
		}

	}

	public static string Get_Appx_AssemblyDirectory(Assembly assembly)
	{
		string assemblyLocation = assembly.Location;
		string directoryPath = Path.GetDirectoryName(assemblyLocation);

		return directoryPath ?? throw new DirectoryNotFoundException("Publish directory not found");

	}
	public static string GetFullPathToExe()
	{
		string path = AppDomain.CurrentDomain.BaseDirectory;
		int pos = path.LastIndexOf("\\");
		return path[..pos];
	}

	public static string GetFullPathToAsset(string assetName)
	{
		return GetFullPathToExe() + "\\Assets\\" + assetName;
	}

	private void OnProcessExit(object sender, EventArgs e)
	{
		if (_webAppProcess != null && !_webAppProcess.HasExited)
		{
			_webAppProcess.Kill();
		}
		NotificationManager.Unregister();
	}
	private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		if (!AppService.IsAppGoingToClose)
		{
			ExceptionLogger.LogException(e.Exception);
		}

	}

	public static string GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
	{
		try
		{
			List<Riverside.Graphite.Core.User> users = JsonSerializer.Deserialize<List<Riverside.Graphite.Core.User>>(File.ReadAllText(Path.Combine(coreFolderPath, "UsrCore.json")));

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

		CancellationToken InstanceCreationToken = AppService.CancellationToken = CancellationToken.None;

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

		AppInstance currentInstance = AppInstance.GetCurrent();
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
					AppNotificationActivatedEventArgs notificationActivatedEventArgs = (AppNotificationActivatedEventArgs)activationArgs.Data;
					NotificationManager.ProcessLaunchActivationArgs(notificationActivatedEventArgs);
				}
			}
		}
	}

	public Window m_window;
}