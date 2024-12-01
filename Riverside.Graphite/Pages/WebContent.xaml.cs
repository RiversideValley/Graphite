using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.CoreUi;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Runtime.ShareHelper;
using Riverside.Graphite.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using WinRT.Interop;
using static Riverside.Graphite.MainWindow;

namespace Riverside.Graphite.Pages
{
	public sealed partial class WebContent : Page
	{
		private Passer param;
		public static bool IsIncognitoModeEnabled { get; set; } = false;
		public BitmapImage PictureWebElement { get; set; }
		public WebView2 WebView { get; private set; }
		private SettingsService SettingsService { get; }
		private AdBlockerWrapper AdBlockerService { get; }
		private readonly SpeechSynthesizer synthesizer = new();
		private bool isOffline = false;

		public WebContent()
		{
			SettingsService = App.GetService<SettingsService>();
			AdBlockerService = App.GetService<AdBlockerWrapper>();

			InitializeComponent();
			WebView = WebViewElement;
			Init();
		}

		private void Init()
		{
			User currentUser = AuthService.IsUserAuthenticated ? AuthService.CurrentUser : null;

			if (currentUser == null || !AuthService.Authenticate(currentUser.Username))
			{
				return;
			}

			string browserFolderPath = Path.Combine(UserDataManager.CoreFolderPath, "Users", currentUser.Username, "Browser");
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", browserFolderPath);
			Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msSingleSignOnOSForPrimaryAccountIsShared");
		}

		private async Task AfterComplete()
		{
			if (IsIncognitoModeEnabled) return;

			await Task.Delay(500);
			User username = AuthService.CurrentUser;
			string source = WebViewElement.Source.ToString();
			string title = WebViewElement.CoreWebView2?.DocumentTitle;

			var dbContext = new HistoryActions(username.Username);
			await dbContext.InsertHistoryItem(source, title, 0, 0, 0);

			if (source is not null)
				UpdateSecurityInfo(source);
		}

		private void UpdateSecurityInfo(string source)
		{
			string isSecure = source.StartsWith("https://") ? "\uE72E" :
							  source.StartsWith("http://") ? "\uE785" : "";
			param.ViewModel.SecurityIcon = isSecure;
			param.ViewModel.SecurityIcontext = isSecure switch
			{
				"\uE72E" => "HTTPS Secured Website",
				"\uE785" => "HTTP Unsecured Website",
				_ => ""
			};
		}

		private void LoadSettings()
		{
			CoreWebView2Settings webViewSettings = WebViewElement.CoreWebView2.Settings;
			Settings coreSettings = SettingsService.CoreSettings;

			webViewSettings.IsScriptEnabled = !coreSettings.DisableJavaScript;
			webViewSettings.IsPasswordAutosaveEnabled = !coreSettings.DisablePassSave;
			webViewSettings.IsGeneralAutofillEnabled = !coreSettings.DisableGenAutoFill;
			webViewSettings.IsWebMessageEnabled = !coreSettings.DisableWebMess;
			webViewSettings.AreBrowserAcceleratorKeysEnabled = coreSettings.BrowserKeys;
			webViewSettings.IsStatusBarEnabled = coreSettings.StatusBar;
			webViewSettings.AreDefaultScriptDialogsEnabled = coreSettings.BrowserScripts;

			SetTrackingPreventionLevel(coreSettings.TrackPrevention);
		}

		private void SetTrackingPreventionLevel(int level)
		{
			WebViewElement.CoreWebView2.Profile.PreferredTrackingPreventionLevel = level switch
			{
				0 => CoreWebView2TrackingPreventionLevel.None,
				1 => CoreWebView2TrackingPreventionLevel.Basic,
				2 => CoreWebView2TrackingPreventionLevel.Balanced,
				3 => CoreWebView2TrackingPreventionLevel.Strict,
				_ => CoreWebView2TrackingPreventionLevel.Balanced
			};

			WebViewElement.CoreWebView2.SetVirtualHostNameToFolderMapping("fireapp.msal", "Assets/WebView/AppFrontend", CoreWebView2HostResourceAccessKind.Allow);
		}

		private void ShareUi(string url, string title)
		{
			nint hWnd = WindowNative.GetWindowHandle((Application.Current as App)?.m_window as MainWindow);
			ShareUIHelper.ShowShareUIURL(url, title, hWnd);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			AdBlockerService.Unregister();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			param = e.Parameter as Passer;

			await WebViewElement.EnsureCoreWebView2Async();

			LoadSettings();

			if (param?.Param != null)
			{
				WebViewElement.CoreWebView2.Navigate(param.Param.ToString());
			}

			WebView2 s = WebViewElement;

			string userAgent = SettingsService.CoreSettings?.Useragent ?? "1";

			if (!string.IsNullOrEmpty(userAgent) && userAgent.Contains("Edg/"))
			{
				s.CoreWebView2.Settings.UserAgent = userAgent[..userAgent.IndexOf("Edg/")];
			}

			await SetupEventHandlersAsync(s);
		}

		private async Task SetupEventHandlersAsync(WebView2 s)
		{
			s.CoreWebView2.ContainsFullScreenElementChanged += (sender, args) =>
			{
				MainWindow window = (Application.Current as App)?.m_window as MainWindow;
				window.GoFullScreenWeb(s.CoreWebView2.ContainsFullScreenElement);
			};

			s.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
			s.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
			s.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
			s.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
			s.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
			s.CoreWebView2.ScriptDialogOpening += ScriptDialogOpening;
			s.CoreWebView2.DocumentTitleChanged += DocumentTitleChanged;
			s.CoreWebView2.FaviconChanged += FaviconChanged;
			s.CoreWebView2.NavigationStarting += NavigationStarting;
			s.CoreWebView2.HistoryChanged += HistoryChanged;
			s.CoreWebView2.NavigationCompleted += NavigationCompleted;
			s.CoreWebView2.SourceChanged += SourceChanged;
			s.CoreWebView2.NewWindowRequested += NewWindowRequested;
			s.CoreWebView2.WebResourceRequested += WebResourceRequested;
			s.CoreWebView2.WebResourceResponseReceived += WebResourceResponseReceived;
			s.CoreWebView2.PermissionRequested += PermissionRequested;

			AdBlockerService.Toggle(false);
			await AdBlockerService.Initialize(WebView);
		}

		private async void ScriptDialogOpening(CoreWebView2 sender, CoreWebView2ScriptDialogOpeningEventArgs args)
		{
			var deferral = args.GetDeferral();
			MainWindow window = (Application.Current as App)?.m_window as MainWindow;
			UIScript ui = new($"{sender.DocumentTitle} says", args.Message, window.Content.XamlRoot);
			ContentDialogResult result = await ui.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				sender.Reload();
			}
			deferral.Complete();
		}

		private void DocumentTitleChanged(CoreWebView2 sender, object args)
		{
			if (!IsIncognitoModeEnabled)
			{
				param.Tab.Header = WebViewElement.CoreWebView2.DocumentTitle;
			}
		}

		private async void FaviconChanged(CoreWebView2 sender, object args)
		{
			try
			{
				if (!IsIncognitoModeEnabled)
				{
					BitmapImage bitmapImage = new();
					IRandomAccessStream stream = await sender.GetFaviconAsync(0);

					ImageIconSource iconSource = new() { ImageSource = bitmapImage };
					await bitmapImage.SetSourceAsync(stream ?? await sender.GetFaviconAsync(CoreWebView2FaviconImageFormat.Jpeg));
					param.Tab.IconSource = iconSource;
				}
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
		}

		private void NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{
			ProgressLoading.IsIndeterminate = true;
			ProgressLoading.Visibility = Visibility.Visible;

			if ((TabViewItem)param.TabView.SelectedItem == param.Tab)
			{
				CheckNetworkStatus();
			}
		}

		private async void HistoryChanged(CoreWebView2 sender, object args)
		{
			await Task.Run(async () =>
			{
				await Task.Delay(1800);

				try
				{
					DispatcherQueue.TryEnqueue(async () =>
					{
						using MemoryStream memoryStream = new();
						try
						{
							await sender.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, memoryStream.AsRandomAccessStream());
							memoryStream.Seek(0, SeekOrigin.Begin);

							BitmapImage bitmap = new() { DecodePixelHeight = 512, DecodePixelWidth = 640 };
							await bitmap.SetSourceAsync(memoryStream.AsRandomAccessStream());
							memoryStream.Seek(0, SeekOrigin.Begin);

							PictureWebElement = bitmap;

							MainWindow currentWindow = (Application.Current as App)?.m_window as MainWindow;
							if (currentWindow?.TabViewContainer.SelectedItem is FireBrowserTabViewItem tab && currentWindow.TabContent.Content is WebContent web)
							{
								tab.BitViewWebContent = web.PictureWebElement;
							}
						}
						catch (Exception ex)
						{
							ExceptionLogger.LogException(ex);
							Console.Write($"Error capturing preview of website:\n{ex.Message}");
						}
					});
				}
				catch (Exception ex)
				{
					ExceptionLogger.LogException(ex);
				}
			});

			if ((TabViewItem)param.TabView.SelectedItem == param.Tab)
			{
				if (WebViewElement.CoreWebView2?.Source is not null)
					await AfterComplete();
			}
		}

		private void NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			ProgressLoading.IsIndeterminate = false;
			ProgressLoading.Visibility = Visibility.Collapsed;
		}

		private void SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
		{
			if ((TabViewItem)param.TabView.SelectedItem == param.Tab)
			{
				param.ViewModel.CurrentAddress = sender.Source;
			}
		}

		private void NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
		{
			MainWindow window = (Application.Current as App)?.m_window as MainWindow;
			param?.TabView.TabItems.Add(window.CreateNewTab(typeof(WebContent), args.Uri));
			args.Handled = true;
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

				return;
			}

			if (IsLoginRequest(args.Request))
			{
				if (args.Request.Headers.Any(x => x.Key == "X-Microsoft-Account-Single-Sign-On-Cookies"))
				{
					AppService.IsAppUserAuthenicated = true;
				}
			}
		}

		private async void WebResourceResponseReceived(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
		{
			if (IsLoginRequest(e.Request))
			{
				CoreWebView2WebResourceResponseView response = e.Response;
				if (response.StatusCode == 200 && IsLoginSuccessful(e.Response))
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

		private async void PermissionRequested(CoreWebView2 sender, CoreWebView2PermissionRequestedEventArgs args)
		{
			args.Handled = true;
			var deferral = args.GetDeferral();

			try
			{
				await DispatcherQueue.EnqueueAsync(async () =>
				{
					string url = WebViewElement.CoreWebView2.Source;
					string username = AuthService.CurrentUser?.Username ?? "default";

					CoreWebView2PermissionState permissionState = await PermissionManager.GetEffectivePermissionState(username, url, args.PermissionKind);

					if (permissionState == CoreWebView2PermissionState.Default)
					{
						// Only show the dialog if there's no stored permission
						permissionState = await PermissionManager.HandlePermissionRequest(username, url, args.PermissionKind);
					}

					args.State = permissionState;
				});
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in PermissionRequested: {ex}");
				args.State = CoreWebView2PermissionState.Deny;
			}
			finally
			{
				deferral.Complete();
			}
		}
		private bool IsLoginRequest(CoreWebView2WebResourceRequest request)
		{
			string[] loginUrls = { "https://login.live.com/login", "https://login.microsoftonline.com/login", "https://login.microsoftonline.com/common/oauth2/authorize", "https://login.microsoftonline.com/common/oauth2/v2.0/token" };
			return loginUrls.Any(loginUrl => request.Uri.StartsWith(loginUrl, StringComparison.OrdinalIgnoreCase));
		}

		private bool IsLoginSuccessful(CoreWebView2WebResourceResponseView response)
		{
			try
			{
				// ignore case some providers use Set-Cookie & others set-cookie capture all 
				return response.Headers.Any(head => head.Key.ToLower() == "set-cookie");
			}
			catch
			{
				return false;
			}
		}

		private bool IsLogoutRequest(CoreWebView2WebResourceRequest request)
		{
			string[] logoutUrls = { "https://login.live.com/logout", "https://login.microsoftonline.com/logout", "https://login.microsoftonline.com/common/oauth2/logout", "https://login.microsoftonline.com/common/oauth2/v2.0/logout?", "https://login.microsoftonline.com/common/oauth2/logoutsession" };
			return logoutUrls.Any(logoutUrl => request.Uri.StartsWith(logoutUrl, StringComparison.OrdinalIgnoreCase));
		}

		private void CoreWebView2_DownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
		{
			MainWindow mainWindow = (Application.Current as App)?.m_window as MainWindow;

			mainWindow.DownloadFlyout.DownloadItemsListView.Items.Insert(0, new DownloadItem(args.DownloadOperation));
			mainWindow.DownloadFlyout.ShowAt(mainWindow.DownBtn);

			args.Handled = true;
		}

		private string SelectionText;
		private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
		{
			var ctx = (Microsoft.UI.Xaml.Controls.CommandBarFlyout)Resources["Ctx"];
			OpenLinks.Visibility = Visibility.Collapsed;
			FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(WebViewElement);

			FlyoutShowOptions options = new()
			{
				Position = args.Location,
				ShowMode = FlyoutShowMode.Standard
			};

			if (args.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.SelectedText)
			{
				SelectionText = args.ContextMenuTarget.SelectionText;
			}
			else if (args.ContextMenuTarget.HasLinkUri)
			{
				SelectionText = args.ContextMenuTarget.LinkUri;
				OpenLinks.Visibility = Visibility.Visible;
			}

			flyout ??= ctx;
			flyout.ShowAt(WebViewElement, options);
			args.Handled = true;
		}

		private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not AppBarButton { Tag: not null } button)
			{
				return;
			}

			CoreWebView2 webview = WebViewElement.CoreWebView2;

			switch (button.Tag)
			{
				case "MenuBack" when WebViewElement.CanGoBack: webview.GoBack(); break;
				case "Forward" when WebViewElement.CanGoForward: webview.GoForward(); break;
				case "Source": webview.OpenDevToolsWindow(); break;
				case "Select": _ = await webview.ExecuteScriptAsync("document.execCommand('selectAll', false, null);"); break;
				case "Copy": ClipBoard.WriteStringToClipboard(SelectionText); break;
				case "Taskmgr": webview.OpenTaskManagerWindow(); break;
				case "Save": HandleSaveAsync(); break;
				case "Share": ShareUi(webview.DocumentTitle, webview.Source); break;
				case "Print": webview.ShowPrintUI(CoreWebView2PrintDialogKind.Browser); break;
			}

			Ctx.Hide();
		}

		private async void HandleSaveAsync()
		{
			_ = WebViewElement.CoreWebView2.DocumentTitle;
			using IRandomAccessStream fileStream = await WebViewElement.CoreWebView2.PrintToPdfStreamAsync(null);
			using DataReader reader = new(fileStream.GetInputStreamAt(0));
			GC.Collect();
		}

		private async void ConvertTextToSpeech(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}

			string lang = SettingsService.CoreSettings.Lang;
			string gender = SettingsService.CoreSettings.Gender;

			string voiceName = gender switch
			{
				"Male" => $"Microsoft Server Speech Text to Speech Voice ({lang}, Mark)",
				"Female" => $"Microsoft Zira",
				_ => throw new ArgumentException("Invalid gender selection")
			};

			string ssml = $"<speak version='1.0' xml:lang='{lang}'><voice name='{voiceName}'>{text}</voice></speak>";

			SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeSsmlToStreamAsync(ssml);

			MediaPlayer mediaPlayer = new()
			{
				Source = MediaSource.CreateFromStream(synthesisStream, synthesisStream.ContentType)
			};

			mediaPlayer.MediaEnded += (_, args) => mediaPlayer.Dispose();

			mediaPlayer.Play();
		}

		public static async void OpenNewWindow(Uri uri)
		{
			_ = await Windows.System.Launcher.LaunchUriAsync(uri);
		}

		private void ContextClicked_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem button && button.Tag != null)
			{
				MainWindow mainWindow = (Application.Current as App)?.m_window as MainWindow;

				switch (button.Tag)
				{
					case "Read":
						ConvertTextToSpeech(SelectionText);
						break;
					case "WebApp":
						// Handle WebApp functionality
						break;
					case "OpenInTab":
						if (IsIncognitoModeEnabled)
						{
							FireBrowserTabViewItem newTab = mainWindow?.CreateNewIncog(typeof(WebContent), new Uri(SelectionText));
							mainWindow?.Tabs.TabItems.Add(newTab);
						}
						else
						{
							FireBrowserTabViewItem newTab = mainWindow?.CreateNewTab(typeof(WebContent), new Uri(SelectionText));
							mainWindow?.Tabs.TabItems.Add(newTab);
						}
						if (SettingsService.CoreSettings.OpenTabHandel)
						{
							select();
						}
						break;
					case "OpenInWindow":
						OpenNewWindow(new Uri(SelectionText));
						break;
					case "OpenInPop":
						OpenPopUpView(new Uri(SelectionText));
						break;
				}
			}
			Ctx.Hide();
		}

		private void OpenPopUpView(Uri uri)
		{
			PopUpView popUpView = new();
			popUpView.SetSource(uri);
			Canvas cv = new();
			cv.Children.Add(popUpView);
			Main.Children.Add(cv);
			popUpView.Show();
		}

		public void select()
		{
			((Application.Current as App)?.m_window as MainWindow)?.SelectNewTab();
		}

		private async void CheckNetworkStatus()
		{
			// this was freezing up the webview changed / if internet do nothing 

			bool isInternetAvailable = NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable;

			if (isInternetAvailable)
				return;

			using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
			try
			{
				while (!isInternetAvailable)
				{
					if (isInternetAvailable && isOffline)
					{
						WebViewElement.Reload();
						Grid.Visibility = Visibility.Visible;
						offlinePage.Visibility = Visibility.Collapsed;
						isOffline = false;
					}
					else if (!isInternetAvailable)
					{
						offlinePage.Visibility = Visibility.Visible;
						Grid.Visibility = Visibility.Collapsed;
						isOffline = true;
					}
					await Task.Delay(100, cts.Token);
				}
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("The task was canceled due to timeout.");
			}
		}
	}
}

