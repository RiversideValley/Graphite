using System;
using CommunityToolkit.Mvvm.Messaging;
using Graphite.Helpers;
using Graphite.ViewModels;
using Graphite.WindowCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;

namespace Graphite;

public partial class App : Application
{
	public static new App Current => (App)Application.Current;
	public NotificationManager NotificationManager { get; private set; }
	public IServiceProvider Services { get; private set; }
	public static Window? MainWindow { get; set; }

	public IWindowHandler WindowHandler { get; private set; }


	public static T GetService<T>() where T : class
	{
		return App.Current is null || App.Current.Services is null
			? throw new NullReferenceException("Application or Services are not properly initialized.")
			: App.Current.Services.GetService(typeof(T)) is not T service
			? throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.")
			: service;
	}

	private IServiceProvider ConfigureServices()
	{
		ServiceCollection services = new();

		_ = services.AddSingleton<WeakReferenceMessenger>();
		_ = services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider =>
			provider.GetRequiredService<WeakReferenceMessenger>());
		_ = services.AddTransient<NewTabViewModel>();
		_ = services.AddTransient<WebContentViewModel>();
		_ = services.AddTransient<HomeViewModel>();
		_ = services.AddTransient<HomeWindowViewModel>();
		_ = services.AddTransient<TabViewItemViewModel>();
		_ = services.AddSingleton<IWindowHandler, WindowHandler>();


		return services.BuildServiceProvider();
	}

	public App()
	{
		this.InitializeComponent();
		Services = ConfigureServices();
		NotificationManager = new NotificationManager();
		WindowHandler = Services.GetRequiredService<IWindowHandler>();
		SetEnvironmentVariables();
	}

	private void SetEnvironmentVariables()
	{
		Environment.SetEnvironmentVariable("WEBVIEW2_USE_VISUAL_HOSTING_FOR_OWNED_WINDOWS", "1");
		Environment.SetEnvironmentVariable("WEBVIEW2_CHANNEL_SEARCH_KIND", "1");
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--window-size=0,0 --window-position=40000,40000");
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-extensions");
	}


	protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
	{
		MainWindow = new MainWindow();

		// Set up the window properties
		WindowHandler.Initialize(MainWindow);
		WindowHandler.SetWindowBackdrop(BackdropType.MicaAlt);
		WindowHandler.RestoreWindowPosition();
		WindowHandler.SetIcon("Logo.ico");
		WindowHandler.SetWindowSize(1300, 1100); // Set your desired default size

		// Activate the window
		MainWindow.Activate();

		AppInstance currentInstance = AppInstance.GetCurrent();
		if (currentInstance.IsCurrent)
		{
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
}

