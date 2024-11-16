using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;



namespace FireAuthService;

public class Worker : BackgroundService
{
	private readonly HttpListener _listener;
	private static IPublicClientApplication? _app;
	private static readonly string[] _scopes = new string[] { "user.read" };
	protected string ClientId { get; } = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";
	protected string RedirectUri { get; } = "urn:ietf:wg:oauth:2.0:oob";
	protected string TenantId { get; } = "f0d59e50-f344-4cbc-b58a-37a7ffc5a17f";

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetForegroundWindow();

	public Worker()
	{
		_listener = new HttpListener();
		_listener.Prefixes.Add("http://+:12221/");
		_ = new MyIdentityLogger();

		try
		{
			_app = PublicClientApplicationBuilder.Create(ClientId)
			.WithAuthority(new Uri($"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?"))
			.WithDefaultRedirectUri()
			.WithLogging(new MyIdentityLogger(), enablePiiLogging: false)
			.WithCacheOptions(new CacheOptions(true))
			.WithTenantId(TenantId)
			.Build();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
			throw;
		}
	}

	private class MyIdentityLogger : IIdentityLogger
	{
		public EventLogLevel MinLogLevel { get; }

		public MyIdentityLogger()
		{
			//Retrieve the log level from an environment variable
			string? msalEnvLogLevel = Environment.GetEnvironmentVariable("MSAL_LOG_LEVEL");

			if (Enum.TryParse(msalEnvLogLevel, out EventLogLevel msalLogLevel))
			{
				MinLogLevel = msalLogLevel;
			}
			else
			{
				//Recommended default log level
				MinLogLevel = EventLogLevel.Informational;
			}
		}

		public bool IsEnabled(EventLogLevel eventLogLevel)
		{
			return eventLogLevel <= MinLogLevel;
		}

		public void Log(LogEntry entry)
		{
			//Log Message here:
			Console.WriteLine(entry.Message);
		}
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			_listener.Start();
			Console.WriteLine("Listening for HTTP requests...");

			while (!stoppingToken.IsCancellationRequested)
			{
				HttpListenerContext context = await _listener.GetContextAsync();

				if (context.Request.Url?.AbsolutePath == "/auth")
				{
					//   await HandleAuthRequest(context);
					string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Pages", "login.html");
					await HandleHtmlRequest(context, filePath);
				}
				else if (context.Request.Url?.AbsolutePath == "/about")
				{
					context.Response.Redirect("https://apps.microsoft.com/detail/9pcn40xxvcvb?hl=en-us&gl=US");
					context.Response.Close();
				}
				else if (context.Request.Url?.AbsolutePath == "/favicon.ico")
				{
					string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "fincog.png");
					await HandleImageRequest(context, filePath);
				}
				else if (context.Request.Url?.AbsolutePath == "/index")
				{
					string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Pages", "main.html");
					await HandleHtmlRequest(context, filePath);
				}
				else if (context.Request.Url?.AbsolutePath == "/repo")
				{
					context.Response.Redirect("https://github.com/FirebrowserDevs");
					context.Response.Close();
				}
				else
				{
					string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Pages", "notfound.html");
					await HandleHtmlRequest(context, filePath);
				}
			}

			_listener.Stop();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message?.ToString());
			Environment.Exit(0);
		}
	}

	private async Task HandleImageRequest(HttpListenerContext context, string _filepath)
	{
		if (File.Exists(_filepath))
		{
			string htmlContent = await File.ReadAllTextAsync(_filepath); context.Response.ContentType = "image/x-icon";
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(htmlContent); context.Response.ContentLength64 = buffer.Length;
			await context.Response.OutputStream.WriteAsync(buffer);
			context.Response.OutputStream.Close();
		}
		else { context.Response.StatusCode = (int)HttpStatusCode.NotFound; context.Response.Close(); }
	}
	private async Task HandleHtmlRequest(HttpListenerContext context, string _filepath)
	{
		if (File.Exists(_filepath))
		{
			string htmlContent = await File.ReadAllTextAsync(_filepath); context.Response.ContentType = "text/html";
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(htmlContent); context.Response.ContentLength64 = buffer.Length;
			await context.Response.OutputStream.WriteAsync(buffer);
			context.Response.OutputStream.Close();
		}
		else { context.Response.StatusCode = (int)HttpStatusCode.NotFound; context.Response.Close(); }
	}
	private async Task HandleAuthRequest(HttpListenerContext context)
	{
		AuthenticationResult result = await _app.AcquireTokenInteractive(_scopes).ExecuteAsync();

		if (result != null)
		{
			Cookie cookie = new("msalToken", result.AccessToken)
			{
				HttpOnly = true,
				Secure = true,
				Expires = DateTime.UtcNow.AddHours(1)
			};

			context.Response.Cookies.Add(cookie);

			string authUri = "https://account.microsoft.com/profile/";
			context.Response.Redirect(authUri);
			context.Response.Close();
		}
		else
		{
			context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			context.Response.Close();
		}
	}

	private async Task ProcessRequest(HttpListenerContext context)
	{
		_ = context.Request;
		HttpListenerResponse response = context.Response;

		string responseString = "<html><title>Hello from FireBrowser Auth Servcie</title></html>";
		byte[] buffer = Encoding.UTF8.GetBytes(responseString);

		response.ContentLength64 = buffer.Length;
		await response.OutputStream.WriteAsync(buffer);
		response.OutputStream.Close();
	}
}



