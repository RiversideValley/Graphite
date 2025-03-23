using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Core.Helper.Logging;
using System.Threading;
using System.Threading.Tasks;

public class TabStateBackgroundService : BackgroundService
{
    private readonly TabManager _tabManager;
    private readonly string _username;
	private readonly ILogger<TabStateBackgroundService> _mangerLogger; 
	public TabStateBackgroundService(TabManager tabManager, string username)
    {
		_mangerLogger = new TabMangerLogger<TabStateBackgroundService>();	
		_tabManager = tabManager;
        _username = username;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
			await Task.Delay(60000, stoppingToken); // Save tab state every 60 seconds

			var result = await _tabManager.SaveTabStateAsync(_username);
			_mangerLogger.LogInformation(result);

			
        }

		_mangerLogger.LogCritical("TabStateBackgroundService has been stopped");
		
	}
}
