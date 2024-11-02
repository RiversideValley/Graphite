using Azure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using FireBrowserWinUi3.Pages.Patch;
using FireBrowserWinUi3.Services.Messages;
using FireBrowserWinUi3Core.CoreUi;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WinRT.Interop;
using static FireBrowserWinUi3.Services.ProcessStarter;
using static System.Formats.Asn1.AsnWriter;
using static System.Windows.Forms.AxHost;

namespace FireBrowserWinUi3.Services.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient
{
    internal MainWindow MainView { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMsLoginVisibility))]
    public bool _IsMsLogin = AppService.IsAppUserAuthenicated;

    public BitmapImage MsProfilePicture { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MsOptionsWebCommand))]
    private ListViewItem _MsOptionSelected;

    //public Visibility _IsMsLoginVisible  => IsMsLogin ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsMsLoginVisibility { get { return IsMsLogin ? Visibility.Visible : Visibility.Collapsed; ; } set { } }
    public Visibility IsMsButtonVisibility { get { return !IsMsLogin ? Visibility.Visible : Visibility.Collapsed; ; } set { } }

    [ObservableProperty] private BitmapImage _profileImage;

    public MainWindowViewModel(IMessenger messenger) : base(messenger)
    {
        Messenger.Register<Message_Settings_Actions>(this, ReceivedStatus);
        //ValidateMicrosoft().ConfigureAwait(false);
    }

    private async Task ValidateMicrosoft()
    {

        IsMsLogin = true; // AppService.MsalService.IsSignedIn;
        if (IsMsLogin)
        {
            if (AppService.GraphService.ProfileMicrosoft is null)
            {


                using (var stream = await AppService.MsalService.GraphClient?.Me.Photo.Content.GetAsync())
                {
                    if (stream == null)
                    {
                        MsProfilePicture = new BitmapImage(new Uri("ms-appx:///Assets/Microsoft.png"));
                        RaisePropertyChanges(nameof(MsProfilePicture));
                        return;
                    }

                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                    MsProfilePicture = bitmapImage;
                    RaisePropertyChanges(nameof(MsProfilePicture));
                }
            }
            else
            {
                MsProfilePicture = AppService.GraphService.ProfileMicrosoft;
                RaisePropertyChanges(nameof(MsProfilePicture));
            }

        }

        RaisePropertyChanges(nameof(IsMsLoginVisibility));
        RaisePropertyChanges(nameof(IsMsButtonVisibility));

    }

    [RelayCommand]
    private async Task LogOut()
    {

        if (MainView.TabWebView is not null)
            MainView.NavigateToUrl("https://login.microsoftonline.com/common/oauth2/v2.0/logout?client_id=edfc73e2-cac9-4c47-a84c-dedd3561e8b5&post_logout_redirect_uri=https://bing.com");
            
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
            try
            {
                while (AppService.IsAppUserAuthenicated)
                {
                    if (!AppService.IsAppUserAuthenicated)
                    {
                        IsMsLogin = false;
                        RaisePropertyChanges(nameof(IsMsLogin));

                        if (MainView.MsLoggedInOptions.IsOpen)
                            MainView.MsLoggedInOptions.Hide();

                        break;
                    }

                    await Task.Delay(400, cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                AppService.IsAppUserAuthenicated = IsMsLogin = false;
                MainView.NavigateToUrl("https://fireapp.msal/main.html");
                MainView.NotificationQueue.Show("You've been logged out of Microsoft", 15000, "Authorization");
                RaisePropertyChanges(nameof(IsMsLogin));
                Console.WriteLine("The task was canceled due to timeout.");
            }
        }



    }

    [RelayCommand]
    private async Task AdminCenter()
    {

        if (!AppService.MsalService.IsSignedIn)
        {
            var answer = await AppService.MsalService.SignInAsync();

            if (answer is null)
            {
                MainView.NotificationQueue.Show("You must sign into the FireBrowser Application for cloudbackups !", 1000, "Backups");
                return;
            }
        }

        var win = new UpLoadBackup();
        win.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay);
        var desktop = await Windowing.SizeWindow();
        win.AppWindow.MoveAndResize(new(MainView.AppWindow.Position.X, 0, desktop.Value.Width / 2, desktop.Value.Height / 2));
        win.ExtendsContentIntoTitleBar = true;
        Windowing.ShowWindow(WindowNative.GetWindowHandle(win), Windowing.WindowShowStyle.SW_SHOWDEFAULT);
        Windowing.AnimateWindow(WindowNative.GetWindowHandle(win), 2000, Windowing.AW_BLEND | Windowing.AW_VER_POSITIVE | Windowing.AW_ACTIVATE);
        win.AppWindow?.ShowOnceWithRequestedStartupState();

    }

    [RelayCommand]
    private Task MsOptionsWeb(object sender)
    {
        try
        {
            MainView?.NavigateToUrl((sender as ListViewItem).Tag.ToString());
            if (MainView.MsLoggedInOptions.IsOpen)
                MainView.MsLoggedInOptions.Hide();

        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
            Messenger.Send(new Message_Settings_Actions("Can't navigate to the requested website", EnumMessageStatus.Informational));
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task LoginToMicrosoft(object sender)
    {
        if (!AppService.IsAppUserAuthenicated)
            // MainView.NavigateToUrl("https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=edfc73e2-cac9-4c47-a84c-dedd3561e8b5&scope=openid profile offline_access&redirect_uri=https://account.microsoft.com/profile/&client-request-id=4cf0210d-abf0-4e60-ab52-5a288c40a636&response_mode=fragment&response_type=code&x-client-SKU=msal.js.browser&x-client-VER=2.37.1&client_info=1&code_challenge=sQi_hISGzmeuS7jjuW-Fvs8CaQ3VrT4piGS_qx_9OVI&code_challenge_method=S256&nonce=1ab7fddc-d3d9-428b-8808-332b3defc041&state=eyJpZCI6ImUxMjQ4MzcwLThiZjYtNGYxZC04MmI5LTY2YWE3MDlkNmM1MiIsIm1ldGEiOnsiaW50ZXJhY3Rpb25UeXBlIjoicmVkaXJlY3QifX0=");
            //MainView.NavigateToUrl("http://localhost:12221/index");
            MainView.NavigateToUrl("https://fireapp.msal/main.html");
        else
        {
            IsMsLogin = true;
            var flyout = MainView.MsLoggedInOptions;
            FlyoutBase.SetAttachedFlyout((sender as Button), MainView.MsLoggedInOptions);
            FlyoutBase.ShowAttachedFlyout((sender as Button));
        }

        return Task.CompletedTask;

    }


    public void RaisePropertyChanges([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(propertyName);
    }

    private void ReceivedStatus(object recipient, Message_Settings_Actions message)
    {
        if (message is null)
            return;

        switch (message.Status)
        {
            case EnumMessageStatus.Login:
                ShowLoginNotification();
                break;
            case EnumMessageStatus.Settings:
                MainView.LoadUserSettings();
                break;
            case EnumMessageStatus.Removed:
                ShowRemovedNotification();
                break;
            case EnumMessageStatus.XorError:
                ShowErrorNotification(message.Payload!);
                break;
            default:
                ShowNotifyNotification(message.Payload!);
                break;

        }
    }

    private void ShowErrorNotification(string _payload)
    {
        var note = new Notification
        {
            Title = "FireBrowserWinUi3 Error \n",
            Message = $"{_payload}",
            Severity = InfoBarSeverity.Error,
            IsIconVisible = true,
            Duration = TimeSpan.FromSeconds(5)
        };
        MainView.NotificationQueue.Show(note);
    }

    private void ShowNotifyNotification(string _payload)
    {
        var note = new Notification
        {
            Title = "FireBrowserWinUi3 Information \n",
            Message = $"{_payload}",
            Severity = InfoBarSeverity.Informational,
            IsIconVisible = true,
            Duration = TimeSpan.FromSeconds(5)
        };
        MainView.NotificationQueue.Show(note);
    }
    private void ShowRemovedNotification()
    {
        var note = new Notification
        {
            Title = "FireBrowserWinUi3 \n",
            Message = $"User has been removed from FireBrowser !",
            Severity = InfoBarSeverity.Warning,
            IsIconVisible = true,
            Duration = TimeSpan.FromSeconds(3)
        };
        MainView.NotificationQueue.Show(note);
    }
    private void ShowLoginNotification()
    {
        var note = new Notification
        {
            Title = "FireBrowserWinUi3 \n",
            Message = $"Welcomes, {AuthService.CurrentUser.Username.ToUpperInvariant()} !",
            Severity = InfoBarSeverity.Informational,
            IsIconVisible = true,
            Duration = TimeSpan.FromSeconds(3)
        };
        MainView.NotificationQueue.Show(note);
    }


}