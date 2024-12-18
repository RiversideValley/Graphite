using FireCore.Data.Database;
using FireCore.Services.Contracts.MessageHandler.AzureTableMessageStorage;
using FireCore.Services.Contracts.MessageHandler;
using FireCore.Services.Contracts.SessionHandler.AzureTableSessionStorage;
using FireCore.Services;
using Microsoft.Identity.Web;
using System.Security.Claims;
using FireCore.Services.Hubs;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using FireCore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;

namespace Riverside.Graphite.Channels
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
			builder.WebHost.UseKestrel(x => x.ListenAnyIP(5000));  
			Startup(builder);
		}

		public static void Startup(WebApplicationBuilder builder) {
			_ = builder.Services.AddHttpClient();
			_ = builder.Services.AddRouting();
			_ = builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation(); 
			_ = builder.Services.AddDistributedMemoryCache();
			_ = builder.Services.AddRazorPages(); 
			_ = builder.Services.AddCors(options => { options.AddDefaultPolicy(builder => { builder.WithOrigins().AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); }); });
			_ = builder.Services.AddSignalR(options =>
			{
				options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(240000);
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

			_ = builder.Services.AddSingleton<IMessageHandler, AzureDataTableMessages>();
			_ = builder.Services.AddSingleton<IAzureDataTableSessionStorage, AzureDataTableSession>();
			_ = builder.Services.AddSingleton<SignalRService>()
						.AddHostedService(sp => sp.GetService<SignalRService>())
						.AddSingleton<IHubContextStore>(sp => sp.GetService<SignalRService>());
			//					_= builder.Services.AddSingleton<SessionContext>();
			_ = builder.Services.AddSingleton<IHubCommander, HubCommander>();

			_ = builder.Services.AddSingleton<AzureChat>();
			_ = builder.Services.AddTransient<MessageBoardViewModel>();
			//_ = builder.Services.AddHostedService<Worker>(); 
			_ = builder.Services.AddWindowsService(op =>
			{
				op.ServiceName = "GraphiteChannel";
			});
			_ = builder.Services.AddLogging(configure => configure.AddConsole())
						.Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Information);

			var app = builder.Build();

			// Configure the HTTP request pipeline.s
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}
			else
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseCors();
			app.UseRouting()
				.UseEndpoints(endpoints =>
				{
					endpoints.MapControllers();
					endpoints.MapRazorPages(); 
					endpoints.MapHub<AzureChat>("/chat");
				});

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

			
			app.Run();
		}
	}
}