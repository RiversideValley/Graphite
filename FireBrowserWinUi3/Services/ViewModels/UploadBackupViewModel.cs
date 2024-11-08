using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireBrowserWinUi3.Services.Models;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3MultiCore;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Email;
using Windows.Storage.Streams;
using Windows.ApplicationModel.DataTransfer;
using System.Net.Http;
using FireBrowserWinUi3.Pages.Patch;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Behaviors;
using Windows.Storage.AccessCache;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Windows.System;
using System.Web;
using Microsoft.Graph.Models;
using Windows.Services.Maps;
using FireBrowserWinUi3Exceptions;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Media;

namespace FireBrowserWinUi3.Services.ViewModels
{
    public partial class UploadBackupViewModel : ObservableRecipient
    {
        public ObservableCollection<EmailUser> Users { get; set; }
        public ObservableCollection<UserEntity> FilesUpload { get; set; }
        public EmailUser SelectedUser { get; set; }
        internal AzBackupService AzBackup { get; set; }

        [ObservableProperty]
        private UserEntity _FileSelected;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFileNewSas))]
        private UserEntity _FileNewSas;
        public Visibility IsFileNewSas => FileNewSas is null ? Visibility.Collapsed : Visibility.Visible;
        public UploadBackupViewModel()
        {
            var user = AppService.MsalService.GetUserAccountAsync().GetAwaiter().GetResult();

            if (user is not null)
            {
                SelectedUser = new EmailUser { Name = user.Username, Email = user.Username };
                var connString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] as string;
                AzBackup = new AzBackupService(connString, "storelean", "firebackups", AuthService.CurrentUser ?? new() { Id = Guid.NewGuid(), Username = "Admin", IsFirstLaunch = false });

                var list = AzBackup.GetUploadFileByUser(user.Username);
                FilesUpload = [.. list];
            }
        }

        partial void OnFileSelectedChanged(UserEntity value)
        {
            FileNewSas = null;
        }

        [RelayCommand]
        private async Task SelectFile()
        {
            if (FileSelected is null)
            {
                UpLoadBackup.Instance?.NotificationQueue.Show("Please select a file from the list", 3000, "Backups");
                return;
            }

            var result = await AzBackup.DownloadBackupFile(FileSelected.BlobName);
            if (result is null) return;

            var note = new Notification
            {
                Title = "Backup Manager \n",
                Message = $"Your backup file has been downloaded:\n{FileSelected.Timestamp.Value.ToLocalTime()} Restore Point !",
                Severity = InfoBarSeverity.Informational,
                IsIconVisible = true,
                Duration = TimeSpan.FromSeconds(3)
            };
            UpLoadBackup.Instance?.NotificationQueue.Show(note);

        }

        [RelayCommand]
        private async Task BackUpCore()
        {

            BackUpDialog dlg = new BackUpDialog();
            dlg.XamlRoot = UpLoadBackup.Instance?.GridMainUploadBackup.XamlRoot;
            await dlg.ShowAsync();

        }
        //public async Task SendGraphEmailAsync(string toEmail, string sasUrl)
        //{
        //    var message = new Message
        //    {
        //        Subject = "Hello from FireBrowser Devs Cloud",
        //        Body = new ItemBody
        //        {
        //            ContentType = BodyType.Html,
        //            Content = $"<html><title>Downloadable Cloud Link</title><body><a href='{sasUrl}'>Cloud Backup Download Link</a></body></html>"
        //        },
        //        ToRecipients = new List<Recipient>
        //    {
        //        new Recipient
        //        {
        //            EmailAddress = new EmailAddress
        //            {
        //                Address = toEmail
        //            }
        //        }
        //    }
        //    };

        //    try
        //    {
        //        var user = AppService.MsalService.GetUserAccountAsync().GetAwaiter().GetResult();
        //        await AppService.MsalService.GraphClient.Users[user.Username].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody() { Message = message });

        //    }
        //    catch (Exception e)
        //    {
        //        ExceptionLogger.LogException(e);
        //        throw;
        //    }

        //    Console.WriteLine("Email sent successfully.");
        //}

        //private async Task SendEmailAsync(string toEmail, string sasUrl)
        //{

        //    var email = new EmailMessage();
        //    email.Subject = "Fire Broswer Backup Download";
        //    email.To.Add(new EmailRecipient(toEmail));
        //    email.Body = $"<html><title>Downloadable Cloud Link</title><body><a href='{sasUrl}'>Cloud Backup Download Link</a></body></html>";
        //    email.SentTime = DateTime.Now;
        //    email.Importance = EmailImportance.High;
        //    await EmailManager.ShowComposeNewEmailAsync(email);

        //    UpLoadBackup.Instance?.NotificationQueue.Show("Your download linke was sent to your email address:" + toEmail, 2000, "FireBrowser Backup Download");
        //}

        [RelayCommand]
        private void GenerateAndSend(UserEntity file)
        {
            // no selected file leave

            if (FileSelected is null)
            {
                UpLoadBackup.Instance?.NotificationQueue.Show("Please select a file!", 3000, "Backups");
                return;
            }

            // get ref to file on Azure Blob Storage

            var connString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] as string;
            var storageAccount = CloudStorageAccount.Parse(connString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("firebackups");
            var blob = container.GetBlockBlobReference(FileSelected.BlobName);

            var sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1) // Token valid for 1 hour
            });

            var sasUrl = blob.Uri.AbsoluteUri + sasToken;
            sasUrl.Append('/');
            // Logic to send email
            FileNewSas = FileSelected;
            FileNewSas.BlobUrl = sasUrl;

            OnPropertyChanged(nameof(FileNewSas));
            
            //await SendEmailAsync(SelectedUser.Email, sasUrl.ToString()).ConfigureAwait(false);
            //await SendGraphEmailAsync(SelectedUser.Email, sasUrl.ToString()).ConfigureAwait(false);

        }


    }


    public class EmailUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }


}
