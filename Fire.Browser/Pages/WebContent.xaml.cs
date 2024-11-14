using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Fire.Browser.Core;
using Fire.Core.CoreUi;
using Fire.Core.Exceptions;
using Fire.Core.Helpers;
using Fire.Core.ShareHelper;
using Fire.Data.Core.Actions;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using WinRT.Interop;
using static FireBrowserWinUi3.MainWindow;

namespace FireBrowserWinUi3.Pages;

public sealed partial class WebContent : Page
{
	private Passer param;
	public static bool IsIncognitoModeEnabled { get; set; } = false;
	public BitmapImage PictureWebElement { get; set; }
	public WebView2 WebView { get; set; }
	private SettingsService SettingsService { get; set; }

	public WebContent()
	{
		SettingsService = App.GetService<SettingsService>();
		InitializeComponent();
		WebView = WebViewElement;
		Init();
	}

	private void Init()
	{
		User currentUser = AuthService.IsUserAuthenticated ? AuthService.CurrentUser : null;

		if (currentUser == null)
		{
			return;
		}

		if (!AuthService.Authenticate(currentUser.Username))
		{
			return;
		}

		string browserFolderPath = Path.Combine(UserDataManager.CoreFolderPath, "Users", currentUser.Username, "Browser");
		Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", browserFolderPath);
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msSingleSignOnOSForPrimaryAccountIsShared");
	}

	private async Task AfterComplete()
	{
		if (!IsIncognitoModeEnabled)
		{
			await Task.Delay(500);
			User username = AuthService.CurrentUser;
			string source = WebViewElement.CoreWebView2.Source.ToString();
			string title = WebViewElement.CoreWebView2.DocumentTitle.ToString();

			HistoryActions dbContext = new(username.Username);
			await dbContext.InsertHistoryItem(source, title, 0, 0, 0);

			string isSecure = source.Contains("https") ? "\uE72E" : (source.Contains("http") ? "\uE785" : "");
			param.ViewModel.SecurityIcon = isSecure;
			param.ViewModel.SecurityIcontext = isSecure == "\uE72E" ? "Https Secured Website" : (isSecure == "\uE785" ? "Http UnSecured Website" : "");
		}
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
		Dictionary<int, CoreWebView2TrackingPreventionLevel> levelMappings = new()
		{
		  { 0, CoreWebView2TrackingPreventionLevel.None },
		  { 1, CoreWebView2TrackingPreventionLevel.Basic },
		  { 2, CoreWebView2TrackingPreventionLevel.Balanced },
		  { 3, CoreWebView2TrackingPreventionLevel.Strict }
		};

		if (!levelMappings.TryGetValue(level, out CoreWebView2TrackingPreventionLevel preventionLevel))
		{
			preventionLevel = CoreWebView2TrackingPreventionLevel.Balanced; // default value
		}

		WebViewElement.CoreWebView2.Profile.PreferredTrackingPreventionLevel = preventionLevel;

		WebViewElement.CoreWebView2.SetVirtualHostNameToFolderMapping("fireapp.msal", "Assets/msal", CoreWebView2HostResourceAccessKind.Allow);

	}


	private void ShareUi(string url, string title)
	{
		nint hWnd = WindowNative.GetWindowHandle((Application.Current as App)?.m_window as MainWindow);
		ShareUIHelper.ShowShareUIURL(url, title, hWnd);
	}

	//static int FirstAttempt = 0;
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

		// Event handlers...
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
		s.CoreWebView2.ScriptDialogOpening += async (sender, args) =>
		{
			_ = args.GetDeferral();
			MainWindow window = (Application.Current as App)?.m_window as MainWindow;
			UIScript ui = new($"{sender.DocumentTitle} says", args.Message, window.Content.XamlRoot);
			ContentDialogResult result = await ui.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				sender.Reload();
			}
		};
		s.CoreWebView2.DocumentTitleChanged += (sender, args) =>
		{
			if (!IsIncognitoModeEnabled)
			{
				param.Tab.Header = WebViewElement.CoreWebView2.DocumentTitle;
			}
		};

		s.CoreWebView2.FaviconChanged += async (sender, args) =>
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
		};



		s.CoreWebView2.NavigationStarting += (sender, args) =>
		{

			//s.DispatcherQueue.TryEnqueue(async () =>
			//{

			//    var user = await AppService.MsalService.SignInAsync();
			//    if (user != null)
			//    {
			//        var token = user.AccessToken ?? null;
			//        if (token == null) return;

			//        string script = $"localStorage.setItem('msalToken','{token}'); ";

			//        await sender.ExecuteScriptAsync(script);
			//    }

			//});



			ProgressLoading.IsIndeterminate = true;
			ProgressLoading.Visibility = Visibility.Visible;

			if ((TabViewItem)param.TabView.SelectedItem == param.Tab)
			{
				CheckNetworkStatus();
			}
		};

		s.CoreWebView2.HistoryChanged += async (sender, args) =>
		{
			//optimize with background task, and use dispatcher to be thread safe 
			//youtube doesn't report navigation when view different videos. 
			_ = await Task.Factory.StartNew(async () =>
			{

				await Task.Delay(1800);

				try
				{
					_ = (DispatcherQueue?.TryEnqueue(async () =>
					{

						// Delay for stability

						using MemoryStream memoryStream = new();
						try
						{
							await s.CoreWebView2?.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, memoryStream.AsRandomAccessStream());
							_ = memoryStream.Seek(0, SeekOrigin.Begin);

							BitmapImage bitmap = new() { DecodePixelHeight = 512, DecodePixelWidth = 640 };
							bitmap.SetSource(memoryStream.AsRandomAccessStream());
							_ = memoryStream.Seek(0, SeekOrigin.Begin);

							PictureWebElement = bitmap;

							MainWindow currentWindow = (Application.Current as App)?.m_window as MainWindow;
							if (currentWindow != null && currentWindow.TabViewContainer.SelectedItem is FireBrowserTabViewItem tab && currentWindow.TabContent.Content is WebContent web)
							{
								tab.BitViewWebContent = web.PictureWebElement;
							}
						}
						catch (Exception ex)
						{
							ExceptionLogger.LogException(ex);
							Console.Write($"Error capturing preview of website:\n{ex.Message}");
						}
					}));
				}
				catch (Exception)
				{

					throw;
				}

				return Task.CompletedTask;

			});

			if ((TabViewItem)param.TabView.SelectedItem == param.Tab)
			{
				await AfterComplete();
			}
		};

		s.CoreWebView2.NavigationCompleted += async (sender, args) =>
		{
			ProgressLoading.IsIndeterminate = false;
			ProgressLoading.Visibility = Visibility.Collapsed;

		};

		s.CoreWebView2.SourceChanged += (sender, args) =>
		{
			if ((TabViewItem)param.TabView.SelectedItem == param.Tab)
			{
				param.ViewModel.CurrentAddress = sender.Source;
			}
		};

		s.CoreWebView2.NewWindowRequested += (sender, args) =>
		{
			MainWindow window = (Application.Current as App)?.m_window as MainWindow;
			param?.TabView.TabItems.Add(window.CreateNewTab(typeof(WebContent), args.Uri));
			args.Handled = true;
		};

		s.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

		s.CoreWebView2.WebResourceRequested += (sender, args) =>
		{

			if (IsLogoutRequest(args.Request))
			{
				AppService.IsAppUserAuthenicated = false;
				Console.WriteLine("Usr has logged out. ");
				s.CoreWebView2?.Navigate("https://fireapp.msal/main.html");
				return;
			}

			if (IsLoginRequest(args.Request))
			{
				if (args.Request.Headers.Count() > 0)
				{
					KeyValuePair<string, string> cookie = args.Request.Headers.Where(x => x.Key == "X-Microsoft-Account-Single-Sign-On-Cookies").FirstOrDefault();
					if (cookie.Value != null)
					{
						AppService.IsAppUserAuthenicated = true;
					}
				}
			}

		};
		s.CoreWebView2.WebResourceResponseReceived += async (s, e) =>
		{

			if (IsLoginRequest(e.Request))
			{
				CoreWebView2WebResourceResponseView response = e.Response;
				if (response.StatusCode == 200 && IsLoginSuccessful(response))
				{
					AppService.IsAppUserAuthenicated = true;
					Console.WriteLine("Login successful.");

				}
			}

			// add this msal.account.keys
			// double check logout 

			await s.ExecuteScriptAsync(@"(function() { function findMsalAccountKeys() {
                                                        const keys = [];
                                                        for (let i = 0; i < localStorage.length; i++) {
                                                            const key = localStorage.key(i);
                                                            if (key.includes(""msal.account"")) {
                                                                keys.push({ key: key, value: JSON.parse(localStorage.getItem(key)), keyValue: JSON.parse(localStorage.getItem(JSON.parse(localStorage.getItem(key)))) });
                                                            }
                                                            if (key.includes(""msalToken"")) {
                                                                keys.push({ key: key, value: key, keyValue: localStorage.getItem(key) });
                                                            }
                                                        }
                                                        return keys;
                                                    } return findMsalAccountKeys();})();"
						).AsTask().ContinueWith(keys =>
						{

							// Critical section here
							JToken token = JToken.Parse(keys.Result);

							if (token is JArray array)
							{
								if (array.Count > 0)
								{
									AppService.IsAppUserAuthenicated = true;
								}
							}

						});

			await s.ExecuteScriptAsync(@"(function() { function findMsalAccountKeysSession() {
                                                        const keys = [];
                                                        for (let i = 0; i < sessionStorage.length; i++) {
                                                            const key = sessionStorage.key(i);
                                                            if (key.includes(""msal.account"")) {
                                                                keys.push({ key: key, value: JSON.parse(sessionStorage.getItem(key)), keyValue: JSON.parse(sessionStorage.getItem(JSON.parse(sessionStorage.getItem(key)))) });
                                                            }
                                                            if (key.includes(""msalToken"")) {
                                                                keys.push({ key: key, value: key, keyValue: sessionStorage.getItem(key) });
                                                            }
                                                        }
                                                        return keys;
                                                    } return findMsalAccountKeysSession();})();"
				).AsTask().ContinueWith(keys =>
				{

					// Critical section here
					JToken token = JToken.Parse(keys.Result);

					if (token is JArray array)
					{
						if (array.Count > 0)
						{
							AppService.IsAppUserAuthenicated = true;
						}
					}

				});

		};

		s.CoreWebView2.PermissionRequested += async (sender, args) =>
		{
			args.Handled = true;
			var deferral = args.GetDeferral();

			try
			{
				await DispatcherQueue.EnqueueAsync(async () =>
				{
					string url = WebViewElement.CoreWebView2.Source;
					var storedPermission = PermissionManager.GetStoredPermission(AuthService.CurrentUser.Username, url, args.PermissionKind);

					if (storedPermission.HasValue)
					{
						args.State = storedPermission.Value;
						return;
					}

					string permissionKind = args.PermissionKind.ToString();
					string formattedPermission = FormatPermissionKind(permissionKind);

					var result = await ShowPermissionDialogAsync(formattedPermission);

					CoreWebView2PermissionState permissionState = result switch
					{
						ContentDialogResult.Primary => CoreWebView2PermissionState.Allow,
						ContentDialogResult.Secondary => CoreWebView2PermissionState.Deny,
						_ => CoreWebView2PermissionState.Default
					};

					bool? allowed = permissionState == CoreWebView2PermissionState.Allow ? true :
									permissionState == CoreWebView2PermissionState.Deny ? false :
									(bool?)null;

					PermissionManager.StorePermission(AuthService.CurrentUser.Username, url, args.PermissionKind, allowed);
					await PermissionManager.SavePermissionsAsync(AuthService.CurrentUser.Username);

					args.State = permissionState;
				});
			}
			finally
			{
				deferral.Complete();
			}
		};
		//s.CoreWebView2.WebResourceRequested += async (sender, args) =>
		//{

		//};

	}

	private string FormatPermissionKind(string permissionKind)
	{
		return string.Join(" ", System.Text.RegularExpressions.Regex.Split(permissionKind, @"(?<!^)(?=[A-Z])"));
	}

	private async Task<ContentDialogResult> ShowPermissionDialogAsync(string permission)
	{
		var rootUrl = new Uri(WebViewElement.CoreWebView2.Source).GetLeftPart(UriPartial.Authority);
		var dialog = new ContentDialog
		{
			Title = $"Allow {rootUrl} to access {permission}?",
			PrimaryButtonText = "Allow",
			SecondaryButtonText = "Deny",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
			Content = "Managed in firebrowser://privacy",
			XamlRoot = this.XamlRoot
		};

		return await dialog.ShowAsync();
	}

	private bool IsLoginRequest(CoreWebView2WebResourceRequest request)
	{
		// Define your login URL patterns
		string[] loginUrls = { "https://login.live.com/login", "https://login.microsoftonline.com/login", "https://login.microsoftonline.com/common/oauth2/authorize" };

		// Check if the request URL matches any known login URL patterns
		return loginUrls.Any(loginUrl => request.Uri.StartsWith(loginUrl, StringComparison.OrdinalIgnoreCase));
	}

	private bool IsLoginSuccessful(CoreWebView2WebResourceResponseView response)
	{
		try
		{
			if (response.Headers.Count() > 0)
			{
				KeyValuePair<string, string> c_Set = response.Headers.Where(head => head.Key == "Set-Cookie").FirstOrDefault();
				if (c_Set.Value is not null)
				{
					return true;
				}
			}
		}
		catch (Exception)
		{

			return false;
		}
		
		return false; 
	}

	private bool IsLogoutRequest(CoreWebView2WebResourceRequest request)
	{
		// Define your logout URL patterns
		string[] logoutUrls = { "https://login.live.com/logout", "https://login.microsoftonline.com/logout", "https://login.microsoftonline.com/common/oauth2/logout", "https://login.microsoftonline.com/common/oauth2/v2.0/logout?" };

		// Check if the request URL matches any known logout URL patterns
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
		_ = (Microsoft.UI.Xaml.Controls.CommandBarFlyout)Resources["Ctx"];
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

		flyout ??= (Microsoft.UI.Xaml.Controls.CommandBarFlyout)Resources["Ctx"];
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

	// Declare the SpeechSynthesizer once, outside the method
	private readonly SpeechSynthesizer synthesizer = new();

	private async void ConvertTextToSpeech(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		// Get the selected language and gender from the settings
		string lang = SettingsService.CoreSettings.Lang;
		string gender = SettingsService.CoreSettings.Gender;

		// Construct the voice name based on gender
		string voiceName = gender switch
		{
			"Male" => $"Microsoft Server Speech Text to Speech Voice ({lang}, Mark)", // Use the correct male voice name
			"Female" => $"Microsoft Zira", // Use the correct female voice name
			_ => throw new ArgumentException("Invalid gender selection")
		};

		// Create the SSML string
		string ssml = $"<speak version='1.0' xml:lang='{lang}'><voice name='{voiceName}'>{text}</voice></speak>";

		// Synthesize speech to stream
		SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeSsmlToStreamAsync(ssml);

		// Create and play the media player
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

			switch ((sender as MenuFlyoutItem).Tag)
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

	private bool isOffline = false;
	private async void CheckNetworkStatus()
	{
		while (true)
		{
			bool isInternetAvailable = NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable;
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
				await Task.Delay(1000);
			}
			await Task.Delay(1000);
		}
	}
}
