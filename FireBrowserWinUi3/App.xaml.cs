using CommunityToolkit.Mvvm.Messaging;
using FireBrowserWinUi3.Services;
using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3.Services.ViewModels;
using FireBrowserWinUi3.ViewModels;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Path = System.IO.Path;

namespace FireBrowserWinUi3;
public partial class App : Application
{
    string changeUsernameFilePath = Path.Combine(Path.GetTempPath(), "changeusername.json");
    public new static App Current => (App)Application.Current;
    private string AzureStorage { get; } = "DefaultEndpointsProtocol=https;AccountName=strorelearn;AccountKey=0pt8CYqrqXUluQE3/60q8wobkmYznb9ovHIzztGVOzNxlSa+U8NlY74uwfggd5DfTmGORBLtXpeKEvDYh2ynfQ==;EndpointSuffix=core.windows.net";

    #region DependencyInjection

    public IServiceProvider Services { get; set; }

    public static T GetService<T>() where T : class
    {
        if (App.Current is not App app || App.Current.Services is null)
        {
            throw new NullReferenceException("Application or Services are not properly initialized.");
        }

        if (App.Current.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }
    public IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<WeakReferenceMessenger>();
        services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider =>
            provider.GetRequiredService<WeakReferenceMessenger>());
        services.AddSingleton<DownloadService>();
        services.AddSingleton<MsalAuthService>();
        services.AddSingleton<GraphService>();
        services.AddTransient<DownloadsViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<UploadBackupViewModel>();

        return services.BuildServiceProvider();
    }

    #endregion

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += Current_UnhandledException;
        _ = FireBrowserWinUi3Navigator.TLD.LoadKnownDomainsAsync().ConfigureAwait(false);

        Environment.SetEnvironmentVariable("WEBVIEW2_USE_VISUAL_HOSTING_FOR_OWNED_WINDOWS", "1");
        // to avoid flashing webview on desktop move to lower bottom right, hopefully off screen 
        // ms says in webview runtime in mid oct /nov

        Environment.SetEnvironmentVariable("WEBVIEW2_CHANNEL_SEARCH_KIND", "1");
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--window-size=0,0 --window-position=40000,40000");
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] = AzureStorage;

    }

    private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (!AppService.IsAppGoingToClose)
            FireBrowserWinUi3Exceptions.ExceptionLogger.LogException(e.Exception);
    }

    public static string GetUsernameFromCoreFolderPath(string coreFolderPath, string userName = null)
    {
        try
        {
            var users = JsonSerializer.Deserialize<List<FireBrowserWinUi3MultiCore.User>>(File.ReadAllText(Path.Combine(coreFolderPath, "UsrCore.json")));

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

       
        AppService.CancellationToken = CancellationToken.None;

        try
        {
            AppService.MsalService = App.GetService<MsalAuthService>();
            AppService.GraphService = App.GetService<GraphService>();

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
                await Task.Delay(1500);
            }

            if (AppService.IsAppGoingToClose == true)
                base.Exit();
            else
                base.OnLaunched(args);

        }
        catch (Exception ex)
        {
            await AppService.CloseCancelToken(AppService.CancellationToken);
            ExceptionLogger.LogException(ex);   
        }
        
    }

    public Window m_window;
}
