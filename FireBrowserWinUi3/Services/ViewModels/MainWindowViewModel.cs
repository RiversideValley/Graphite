using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using FireBrowserWinUi3.Services.Messages;
using FireBrowserWinUi3Core.CoreUi;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3MultiCore;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinRT.Interop;
using static FireBrowserWinUi3.Services.ProcessStarter;
using static System.Formats.Asn1.AsnWriter;
using static System.Windows.Forms.AxHost;

namespace FireBrowserWinUi3.Services.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient
{
    internal MainWindow MainView { get; set; }
    public bool IsMsLogin { get; set; }

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
        ValidateMicrosoft().ConfigureAwait(false);
    }

    private async Task ValidateMicrosoft()
    {

        IsMsLogin = AppService.MsalService.IsSignedIn;
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
    private void MsOptionsWeb(object sender)
    {
        try
        {


            MainView?.NavigateToUrl((sender as ListViewItem).Tag.ToString());
            MainView?.btnMsApps.Flyout.Hide();

        }
        catch (Exception)
        {
            Messenger.Send(new Message_Settings_Actions("Can't navigate to the requested website", EnumMessageStatus.Informational));
        }

    }

    [RelayCommand]
    private async Task LoginToMicrosoft()
    {

        var answer = await AppService.MsalService.SignInAsync();

        if (AppService.MsalService.IsSignedIn)
        {
            await ValidateMicrosoft();
        }

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