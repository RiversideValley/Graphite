using FireCore.Data.Database;
using FireCore.Services.Contracts.MessageHandler.AzureTableMessageStorage;
using FireCore.Services.Contracts.MessageHandler;
using FireCore.Services.Contracts.SessionHandler.AzureTableSessionStorage;
using FireCore.Services;
using Microsoft.Identity.Web;
using System.Security.Claims;
using FireCore.Services.Hubs;

namespace Riverside.Graphite.Channels
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
		

			return Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					_ = services.AddHttpClient(); 
					_ = services.AddDistributedMemoryCache();
					_ = services.AddSignalR(options =>
					{
						options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(120000);
						options.KeepAliveInterval = TimeSpan.FromMilliseconds(840000);
					}).AddAzureSignalR(options =>
					{
						//  This is a tircky way to associate user name with connection for sample purpose.
						//  For PROD, we suggest to use authentication and authorization, see here:
						//  https://docs.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-2.2
						options.ClaimsProvider = context =>
						[
							 new Claim(ClaimTypes.NameIdentifier, context?.Request?.Query["username"]!)
						];
						options.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Preferred;

					});

					_ = services.AddSingleton<IMessageHandler, AzureTableMessageStorage>();
					_ = services.AddSingleton<IAzureDataTableSessionStorage, AzureDataTableSession>();
					_ = services.AddSingleton<SignalRService>()
								.AddHostedService(sp => sp.GetService<SignalRService>())
								.AddSingleton<IHubContextStore>(sp => sp.GetService<SignalRService>());
					//					_= services.AddSingleton<SessionContext>();
					_ = services.AddSingleton<IHubCommander, HubCommander>();
				
					_= services.AddSingleton<AzureChat>();
					_ = services.AddHostedService<Worker>(); 
					_ = services.AddWindowsService(op =>
					{
						op.ServiceName = "GraphiteChannel";
					});
					_ = services.AddLogging(configure => configure.AddConsole())
								.Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Information);
				}).ConfigureLogging(options =>
				{
					_ = options.AddEventSourceLogger();
				});
		}
	}
}