using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Windows.ApplicationModel.Background;

namespace FireBrowserWinUi3;


public sealed class BackFireAuthService : IBackgroundTask
{
    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        try
        {
            // Your background task logic here // Example: Updating UI, fetching data, etc.
            var host = Host.CreateDefaultBuilder().ConfigureServices((hostContext, services) => { services.AddHostedService<FireAuthService.Worker>(); }).Build();
            await host.RunAsync();

        }
        finally { deferral.Complete(); }
    }
}
