using Azure.Core;
using CommunityToolkit.WinUI.Helpers;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Services;
using FireBrowserWinUi3Core.CoreUi;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3Core.ShareHelper;
using FireBrowserWinUi3DataCore.Actions;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using FireBrowserWinUi3MultiCore.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using Windows.UI.WebUI;
using WinRT.Interop;
using static FireBrowserWinUi3.MainWindow;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace FireBrowserWinUi3.Pages;

public sealed partial class WebContent : Page
{
    Passer param;
    public static bool IsIncognitoModeEnabled { get; set; } = false;
    public BitmapImage PictureWebElement { get; set; }
    public WebView2 WebView { get; set; }
    SettingsService SettingsService { get; set; }

    public WebContent()
    {
        SettingsService = App.GetService<SettingsService>();
        this.InitializeComponent();
        WebView = this.WebViewElement;
        Init();
    }

    private void Init()
    {
        var currentUser = AuthService.IsUserAuthenticated ? AuthService.CurrentUser : null;

        if (currentUser == null) return;

        if (!AuthService.Authenticate(currentUser.Username)) return;

        string browserFolderPath = Path.Combine(UserDataManager.CoreFolderPath, "Users", currentUser.Username, "Browser");
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", browserFolderPath);
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msSingleSignOnOSForPrimaryAccountIsShared");
    }

    private async Task AfterComplete()
    {
        if (!IsIncognitoModeEnabled)
        {
            await Task.Delay(500);
            var username = AuthService.CurrentUser;
            var source = WebViewElement.CoreWebView2.Source.ToString();
            var title = WebViewElement.CoreWebView2.DocumentTitle.ToString();

            var dbContext = new HistoryActions(username.Username);
            await dbContext.InsertHistoryItem(source, title, 0, 0, 0);

            var isSecure = source.Contains("https") ? "\uE72E" : (source.Contains("http") ? "\uE785" : "");
            param.ViewModel.SecurityIcon = isSecure;
            param.ViewModel.SecurityIcontext = isSecure == "\uE72E" ? "Https Secured Website" : (isSecure == "\uE785" ? "Http UnSecured Website" : "");
        }
    }

    private void LoadSettings()
    {
        var webViewSettings = WebViewElement.CoreWebView2.Settings;
        var coreSettings = SettingsService.CoreSettings;

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
        var levelMappings = new Dictionary<int, CoreWebView2TrackingPreventionLevel>
        {
          { 0, CoreWebView2TrackingPreventionLevel.None },
          { 1, CoreWebView2TrackingPreventionLevel.Basic },
          { 2, CoreWebView2TrackingPreventionLevel.Balanced },
          { 3, CoreWebView2TrackingPreventionLevel.Strict }
        };

        if (!levelMappings.TryGetValue(level, out var preventionLevel))
        {
            preventionLevel = CoreWebView2TrackingPreventionLevel.Balanced; // default value
        }

        WebViewElement.CoreWebView2.Profile.PreferredTrackingPreventionLevel = preventionLevel;
        
        WebViewElement.CoreWebView2.SetVirtualHostNameToFolderMapping("fireapp.msal", "Assets/msal", CoreWebView2HostResourceAccessKind.Allow);

    }


    private void ShareUi(string url, string title)
    {
        var hWnd = WindowNative.GetWindowHandle((Application.Current as App)?.m_window as MainWindow);
        ShareUIHelper.ShowShareUIURL(url, title, hWnd);
    }

    //static int FirstAttempt = 0;
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        param = e.Parameter as Passer;

        await WebViewElement.EnsureCoreWebView2Async();

        LoadSettings();

        if (param?.Param != null) WebViewElement.CoreWebView2.Navigate(param.Param.ToString());

        WebView2 s = WebViewElement;

        string userAgent = SettingsService.CoreSettings?.Useragent ?? "1";

        if (!string.IsNullOrEmpty(userAgent) && userAgent.Contains("Edg/"))
        {
            s.CoreWebView2.Settings.UserAgent = userAgent.Substring(0, userAgent.IndexOf("Edg/"));
        }

        // Event handlers...
        s.CoreWebView2.ContainsFullScreenElementChanged += (sender, args) =>
        {
            var window = (Application.Current as App)?.m_window as MainWindow;
            window.GoFullScreenWeb(s.CoreWebView2.ContainsFullScreenElement);
        };

        s.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
        s.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        s.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        s.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        s.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
        s.CoreWebView2.ScriptDialogOpening += async (sender, args) =>
        {
            args.GetDeferral();
            var window = (Application.Current as App)?.m_window as MainWindow;
            UIScript ui = new UIScript($"{sender.DocumentTitle} says", args.Message, window.Content.XamlRoot);
            var result = await ui.ShowAsync();

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
                    var bitmapImage = new BitmapImage();
                    var stream = await sender.GetFaviconAsync(0);

                    var iconSource = new ImageIconSource { ImageSource = bitmapImage };
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
            if ((TabViewItem)param.TabView.SelectedItem == param.Tab) await AfterComplete();
        };

        s.CoreWebView2.NavigationCompleted += async (sender, args) =>
        {
            ProgressLoading.IsIndeterminate = false;
            ProgressLoading.Visibility = Visibility.Collapsed;

            //optimize with background task, and use dispatcher to be thread safe 
            await Task.Factory.StartNew(async () =>
            {

                await Task.Delay(2400);

                try
                {
                    DispatcherQueue?.TryEnqueue(async () =>
                    {

                        // Delay for stability

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            try
                            {
                                await s.CoreWebView2?.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, memoryStream.AsRandomAccessStream());
                                memoryStream.Seek(0, SeekOrigin.Begin);

                                BitmapImage bitmap = new BitmapImage { DecodePixelHeight = 512, DecodePixelWidth = 640 };
                                bitmap.SetSource(memoryStream.AsRandomAccessStream());
                                memoryStream.Seek(0, SeekOrigin.Begin);

                                PictureWebElement = bitmap;

                                var currentWindow = (Application.Current as App)?.m_window as MainWindow;
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
                        }
                    });
                }
                catch (Exception)
                {

                    throw;
                }

                return Task.CompletedTask;

            });


        };

        s.CoreWebView2.SourceChanged += (sender, args) =>
        {
            if ((TabViewItem)param.TabView.SelectedItem == param.Tab) param.ViewModel.CurrentAddress = sender.Source;
        };

        s.CoreWebView2.NewWindowRequested += (sender, args) =>
        {
            var window = (Application.Current as App)?.m_window as MainWindow;
            param?.TabView.TabItems.Add(window.CreateNewTab(typeof(WebContent), args.Uri));
            args.Handled = true;
        };

        s.CoreWebView2.WebResourceResponseReceived += async (s, e) =>
        {

            var head = e.Request.Headers;
            var isHeader = head.ToList().Where(t => t.Key == "Authentication");
            if (isHeader.Count() > 0)
            {
                foreach (var item in isHeader.ToList())
                {
                    //background logging
                    await Task.Run(() =>
                    {
                        string json = JsonConvert.SerializeObject(item, Formatting.Indented);
                        //string stdout = $"{DateTimeOffset.Now.ToLocalTime()}-{staticCounter++}:\n{json}";
                        var helper = new JsonHelper(ExceptionLogger.MsalLogFile);
                        helper.SaveToFile(new JArray(json)).WaitAsync(AppService.CancellationToken);

                        return Task.CompletedTask;
                    }).ConfigureAwait(false);
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
                        ).AsTask().ContinueWith(async keys =>
                        {

                            // Critical section here
                            JToken token = JToken.Parse(keys.Result);

                            if (token is JArray array)
                            {
                                if (array.Count > 0)
                                {
                                    AppService.IsAppUserAuthenicated = true;
                                    //background logging
                                    await Task.Run(() =>
                                    {
                                        string json = JsonConvert.SerializeObject(array, Formatting.Indented);
                                        //string stdout = $"{DateTimeOffset.Now.ToLocalTime()}-{staticCounter++}:\n{json}";
                                        var helper = new JsonHelper(ExceptionLogger.MsalLogFile);
                                        helper.SaveToFile(array).WaitAsync(AppService.CancellationToken);


                                        return Task.CompletedTask;
                                    }).ConfigureAwait(false);
                                }
                            }

                        });



            //await s.ExecuteScriptAsync("document.cookie").AsTask().ContinueWith(async cookieTask =>
            //{
            //    var cookies = cookieTask.Result;
            //    if (cookies.Contains("msal.token.keys"))
            //    {
            //        AppService.IsAppUserAuthenicated = true;
            //    }
            //});

        };

        //s.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

        //s.CoreWebView2.WebResourceRequested += async (sender, args) =>
        //{

        //};

    }


    private void CoreWebView2_DownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
    {
        var mainWindow = (Application.Current as App)?.m_window as MainWindow;

        mainWindow.DownloadFlyout.DownloadItemsListView.Items.Insert(0, new DownloadItem(args.DownloadOperation));
        mainWindow.DownloadFlyout.ShowAt(mainWindow.DownBtn);

        args.Handled = true;
    }

    string SelectionText;
    private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        var flyout1 = (Microsoft.UI.Xaml.Controls.CommandBarFlyout)Resources["Ctx"];
        OpenLinks.Visibility = Visibility.Collapsed;
        var flyout = FlyoutBase.GetAttachedFlyout(WebViewElement);

        var options = new FlyoutShowOptions()
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

        flyout = flyout ?? (Microsoft.UI.Xaml.Controls.CommandBarFlyout)Resources["Ctx"];
        flyout.ShowAt(WebViewElement, options);
        args.Handled = true;
    }

    private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not AppBarButton { Tag: not null } button)
            return;

        var webview = WebViewElement.CoreWebView2;

        switch (button.Tag)
        {
            case "MenuBack" when WebViewElement.CanGoBack: webview.GoBack(); break;
            case "Forward" when WebViewElement.CanGoForward: webview.GoForward(); break;
            case "Source": webview.OpenDevToolsWindow(); break;
            case "Select": await webview.ExecuteScriptAsync("document.execCommand('selectAll', false, null);"); break;
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
        string websitetitle = WebViewElement.CoreWebView2.DocumentTitle;
        using (IRandomAccessStream fileStream = await WebViewElement.CoreWebView2.PrintToPdfStreamAsync(null))
        {
            using (var reader = new DataReader(fileStream.GetInputStreamAt(0)))
            {
                GC.Collect();
            }
        }
    }

    // Declare the SpeechSynthesizer once, outside the method
    private SpeechSynthesizer synthesizer = new SpeechSynthesizer();

    private async void ConvertTextToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

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
        var ssml = $"<speak version='1.0' xml:lang='{lang}'><voice name='{voiceName}'>{text}</voice></speak>";

        // Synthesize speech to stream
        var synthesisStream = await synthesizer.SynthesizeSsmlToStreamAsync(ssml);

        // Create and play the media player
        var mediaPlayer = new MediaPlayer
        {
            Source = MediaSource.CreateFromStream(synthesisStream, synthesisStream.ContentType)
        };

        mediaPlayer.MediaEnded += (_, args) => mediaPlayer.Dispose();

        mediaPlayer.Play();
    }

    public static async void OpenNewWindow(Uri uri) => await Windows.System.Launcher.LaunchUriAsync(uri);

    private void ContextClicked_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem button && button.Tag != null)
        {
            var mainWindow = (Application.Current as App)?.m_window as MainWindow;

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
                        var newTab = mainWindow?.CreateNewIncog(typeof(WebContent), new Uri(SelectionText));
                        mainWindow?.Tabs.TabItems.Add(newTab);
                    }
                    else
                    {
                        var newTab = mainWindow?.CreateNewTab(typeof(WebContent), new Uri(SelectionText));
                        mainWindow?.Tabs.TabItems.Add(newTab);
                    }
                    if (SettingsService.CoreSettings.OpenTabHandel) select();
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
        PopUpView popUpView = new PopUpView();
        popUpView.SetSource(uri);
        Canvas cv = new Canvas();
        cv.Children.Add(popUpView);
        Main.Children.Add(cv);
        popUpView.Show();
    }

    public void select() => ((Application.Current as App)?.m_window as MainWindow)?.SelectNewTab();

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
