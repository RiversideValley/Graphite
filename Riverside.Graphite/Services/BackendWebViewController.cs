using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using System;
using System.Threading.Tasks;
using System.Web;

namespace Riverside.Graphite.Services
{
	public class BackendWebViewController : ObservableRecipient
	{
		private readonly ISettingsService SettingsService;
		public WebView2 WebView { get; set; }
		public string TheAddressToGo { get; private set; }

		private BackendWebViewController(ISettingsService settingsService, IMessenger messenger) : base(messenger)
		{
			SettingsService = settingsService;
		}
		public BackendWebViewController() : base()
		{
			WebView = new WebView2();
			WebView.Source = new Uri(TheAddressToGo ?? "https://copilot.microsoft.com/");
			_ = WebView.EnsureCoreWebView2Async().AsTask();
		}
		public async Task<Task> LoadWebSite(string source)
		{
			bool isNavigationCompleted = default;


			try
			{
				WebView = new();
				WebView.Source = new Uri(TheAddressToGo = source);
				await WebView.EnsureCoreWebView2Async();

				DispatcherTimer dispatcherTimer = new();
				dispatcherTimer.Interval = TimeSpan.FromSeconds(60);
				dispatcherTimer.Start();
				dispatcherTimer.Tick += (s, e) =>
				{
					isNavigationCompleted = true;
				};


				WebView.CoreWebView2.NavigationCompleted += (s, e) =>
				{
					isNavigationCompleted = true;
				};


				while (!isNavigationCompleted)
				{
					await Task.Delay(240);
				}

				return Task.CompletedTask;
			}
			catch (Exception e)
			{
				return Task.FromException(e);
				throw;
			}
		}
		internal void SetupWebView()
		{
			WebView.CoreWebView2.NewWindowRequested += (s, e) =>
			{
				string decode = HttpUtility.UrlDecode(e.Uri);
				Uri decodeUri = default;
				if (Uri.TryCreate(decode, UriKind.RelativeOrAbsolute, out decodeUri))
				{
					WebView.Source = decodeUri;
				}
				else
				{
					_ = Messenger.Send(new Message_Settings_Actions("Invalid Url was presented", EnumMessageStatus.XorError));
					e.Handled = false;
				}
				e.Handled = true;
			};
		}
		public async Task Intialize(MainWindow.Passer passer)
		{
			// any service that needs to tracking...
			Uri url = UrlValidater.GetValidateUrl(passer.Param.ToString());
			TheAddressToGo = url is null ? "about:blank" : url.ToString();

			try
			{
				await Task.Delay(80);
				await WebView.EnsureCoreWebView2Async();
				// add or handlers below. 
				SetupWebView();
			}
			catch (Exception)
			{
				return;
			}
		}
	}
}
