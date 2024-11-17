using Microsoft.AspNetCore.Builder;

namespace FireAuthService
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
					_ = services.AddHostedService<Worker>();
					_ = services.AddWindowsService(op =>
					{
						op.ServiceName = "FireAuthService";
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
