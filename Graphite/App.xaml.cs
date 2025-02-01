using System;
using CommunityToolkit.Mvvm.Messaging;
using Graphite.Helpers;
using Graphite.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;

namespace Graphite;

public partial class App : Application
{
	public static new App Current => (App)Application.Current;
	public NotificationManager NotificationManager { get; set; }


	#region DepenjencInjection
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
		_ = services.AddTransient<NewTabViewModel>();
		_ = services.AddTransient<WebContentViewModel>();
		_ = services.AddTransient<HomeViewModel>();
		_ = services.AddTransient<HomeWindowViewModel>();
		_ = services.AddTransient<TabViewItemViewModel>();


		return services.BuildServiceProvider();
	}

	#endregion

	public App()
	{
		this.InitializeComponent();
		EnvorimentVariables();
	}

	public void EnvorimentVariables()
	{
		Environment.SetEnvironmentVariable("WEBVIEW2_USE_VISUAL_HOSTING_FOR_OWNED_WINDOWS", "1");
		Environment.SetEnvironmentVariable("WEBVIEW2_CHANNEL_SEARCH_KIND", "1");
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--window-size=0,0 --window-position=40000,40000");
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-extensions");
	}

    /// <param name="args">Details about the launch request and process.</param>
    public static Window MainWindow { get; set; }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
		App.Current.Services = App.Current.ConfigureServices();
		MainWindow = new MainWindow();
        MainWindow.Activate();

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
}