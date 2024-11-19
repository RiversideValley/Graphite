using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Riverside.Graphite.Core;
using Riverside.Graphite.Pages;
using Riverside.Graphite.Services;
using Riverside.Graphite.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Riverside.Graphite.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MsalAuth : Page
	{
		internal bool IsNavigated { get; set; }
		internal DispatcherTimer timer; 
		private MainWindowViewModel ViewModelMain { get; set; }
		public MsalAuth()
		{
			this.InitializeComponent();
			string browserFolderPath = Path.Combine(UserDataManager.CoreFolderPath, "Users", AuthService.CurrentUser?.Username, "Browser");
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", browserFolderPath);
			Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msSingleSignOnOSForPrimaryAccountIsShared");
			
		}

		public async Task Initialize(MainWindowViewModel mainWindowViewModel)
		{
			ViewModelMain = mainWindowViewModel;
			await webView.EnsureCoreWebView2Async();
			SetWebViewHandler(webView);
			webView.CoreWebView2.SetVirtualHostNameToFolderMapping("fireapp.msal", "Assets/WebView/AppFrontend", CoreWebView2HostResourceAccessKind.Allow);
			webView.CoreWebView2.Navigate("https://fireapp.msal/main.html");
			await Task.Delay(200);			

		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			var vm = (MainWindowViewModel)e.Parameter;
			if (vm != null) {
				await Initialize(vm);
			}


			base.OnNavigatedTo(e);
		}

			
		private void SetWebViewHandler(WebView2 s) {

			s.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
			s.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
			s.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
			s.CoreWebView2.WebResourceRequested += WebResourceRequested;
			s.CoreWebView2.WebResourceResponseReceived += WebResourceResponseReceived;
			s.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
		}

		private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			IsNavigated = true;
		}

		
		private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
		{
			MainWindow window = (Application.Current as App)?.m_window as MainWindow;
			window?.NavigateToUrl(args.Uri);
			
		}

		private void WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
		{
			if (IsLogoutRequest(args.Request))
			{
				AppService.IsAppUserAuthenicated = false;
				Console.WriteLine("User has logged out.");
				MainWindow window = (Application.Current as App)?.m_window as MainWindow;
				window.TabContent.Navigate(typeof(NewTab));
				window.NotificationQueue.Show("You have been logged out of Microsoft", 2000, "Graphite Authorization");
				window.ViewModelMain.IsMsLogin = false;
				window.ViewModelMain.RaisePropertyChanges(nameof(window.ViewModelMain.IsMsLogin));
				//webView.CoreWebView2?.Navigate("https://fireapp.msal/main.html"); 
				return;
			}

			if (IsLoginRequest(args.Request))
			{
				if (args.Request.Headers.Any(x => x.Key == "X-Microsoft-Account-Single-Sign-On-Cookies"))
				{
					AppService.IsAppUserAuthenicated = true;
					MainWindow window = (Application.Current as App)?.m_window as MainWindow;
					window.ViewModelMain.IsMsLogin = true;
					window.ViewModelMain.RaisePropertyChanges(nameof(window.ViewModelMain.IsMsLogin));
				}
			}
		}
		private async void WebResourceResponseReceived(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
		{
			if (IsLoginSuccessful(e.Response))
			{
					CoreWebView2WebResourceResponseView response = e.Response;
					if (response.StatusCode == 200)
					{
						AppService.IsAppUserAuthenicated = true;
						MainWindow window = (Application.Current as App)?.m_window as MainWindow;
						window.ViewModelMain.IsMsLogin = true;
						window.ViewModelMain.RaisePropertyChanges(nameof(window.ViewModelMain.IsMsLogin));
						Console.WriteLine("Login successful.");
					}
				
			}

			string script = @"
            (function() {
                function findMsalKeys(storage) {
                    return Object.keys(storage)
                        .filter(key => key.includes('msal.account') || key.includes('msalToken'))
                        .map(key => ({
                            key: key,
                            value: storage.getItem(key),
                            keyValue: key.includes('msal.account') 
                                ? JSON.parse(storage.getItem(JSON.parse(storage.getItem(key))))
                                : storage.getItem(key)
                        }));
                }
                return {
                    localStorage: findMsalKeys(localStorage),
                    sessionStorage: findMsalKeys(sessionStorage)
                };
            })();";

			try
			{
				var result = await sender.ExecuteScriptAsync(script);
				var tokenData = JObject.Parse(result);

				bool hasLocalStorageKeys = tokenData["localStorage"].HasValues;
				bool hasSessionStorageKeys = tokenData["sessionStorage"].HasValues;

				if (hasLocalStorageKeys || hasSessionStorageKeys)
				{
					AppService.IsAppUserAuthenicated = true;
				}

				if (!AppService.IsAppUserAuthenicated)
				{
					string logoutScript = @"
                    (function() {
                        return {
                            hasLoginButton: !!document.querySelector('button[aria-label=""Login""]'),
                            hasLogoutButton: !!document.querySelector('button[aria-label=""Logout""]')
                        };
                    })();";

					var logoutResult = await sender.ExecuteScriptAsync(logoutScript);
					var logoutData = JObject.Parse(logoutResult);

					if (logoutData["hasLoginButton"].Value<bool>() && !logoutData["hasLogoutButton"].Value<bool>())
					{
						AppService.IsAppUserAuthenicated = false;
						Console.WriteLine("User logged out.");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error checking authentication status: {ex.Message}");
			}
		}

		private bool IsLoginRequest(CoreWebView2WebResourceRequest request)
		{
			string[] loginUrls = { "https://login.live.com/login", "https://login.microsoftonline.com/login", "https://login.microsoftonline.com/common/oauth2/authorize" , "https://graph.microsoft.com/v1.0/users/" };
			return loginUrls.Any(loginUrl => request.Uri.StartsWith(loginUrl, StringComparison.OrdinalIgnoreCase));
		}

		private bool IsLoginSuccessful(CoreWebView2WebResourceResponseView response)
		{
			try
			{
				return response.Headers.Any(head => head.Key == "access-control-allow-headers" || head.Key == "access-control-allow-credentials");
			}
			catch
			{
				return false;
			}
		}

		private bool IsLogoutRequest(CoreWebView2WebResourceRequest request)
		{
			string[] logoutUrls = { "https://login.live.com/logout", "https://login.microsoftonline.com/logout", "https://login.microsoftonline.com/common/oauth2/logout", "https://login.microsoftonline.com/common/oauth2/v2.0/logout?" };
			return logoutUrls.Any(logoutUrl => request.Uri.StartsWith(logoutUrl, StringComparison.OrdinalIgnoreCase));
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			FlyoutBase.SetAttachedFlyout((FrameworkElement)sender, MsLoggedInOptions);
			FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
		}
	}
}
