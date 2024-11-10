using Microsoft.AspNetCore.Builder;

namespace FireAuthService
{
	public class Program
	{
		public static void Main(string[] args)
		{

			CreateHostBuilder(args).Build().Run();

		}
		public static IHostBuilder CreateHostBuilder(string[] args) =>

			Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					services.AddHostedService<Worker>();
					services.AddWindowsService(op =>
					{
						op.ServiceName = "FireAuthService";
					});
					services.AddLogging(configure => configure.AddConsole())
								.Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Information);


				}).ConfigureLogging(options =>
								{
									options.AddEventSourceLogger();
								});


	}
}
