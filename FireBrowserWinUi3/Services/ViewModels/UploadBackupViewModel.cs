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

namespace FireBrowserWinUi3.Services.ViewModels
{
    public partial class UploadBackupViewModel : ObservableRecipient
    {
        public ObservableCollection<EmailUser> Users { get; set; }
        public ObservableCollection<UserEntity> FilesUpload { get; set; }
        public EmailUser SelectedUser { get; set; }
        internal AzBackupService azBackup { get; set; }

        [ObservableProperty]
        private UserEntity _FileSelected;
        public UploadBackupViewModel()
        {
            var user = AppService.MsalService.GetUserAccountAsync().GetAwaiter().GetResult();

            if (user is not null)
            {
                Users = new ObservableCollection<EmailUser>
                {
                    new EmailUser { Name = user.Username, Email = user.Username }
                };

                var connString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] as string;
                azBackup = new AzBackupService(connString, "storelean", "firebackups", AuthService.CurrentUser ?? new() { Id = Guid.NewGuid(), Username = "Admin", IsFirstLaunch = false });

                var list = azBackup.GetUploadFileByUser(user.Username);
                FilesUpload = [.. list];
            }
        }

        [RelayCommand]
        private async Task SelectFile()
        {
            if (FileSelected is null)
            {
                UpLoadBackup.Instance?.NotificationQueue.Show("Please select a file from the list");
                return;
            }

            await azBackup.DownloadBackupFile(FileSelected.BlobName);
            var note = new Notification
            {
                Title = "Backup Manager \n",
                Message = $"Your backup file has been downloaded:\n{FileSelected.Timestamp.Value.ToLocalTime()} Restore Point !",
                Severity = InfoBarSeverity.Informational,
                IsIconVisible = true,
                Duration = TimeSpan.FromSeconds(4)
            };
            UpLoadBackup.Instance?.NotificationQueue.Show(note);

        }
        private async Task SendEmailAsync(string toEmail, string sasUrl)
        {
            var email = new EmailMessage();
            email.Subject = "Bug Report";
            email.To.Add(new EmailRecipient("toEmail"));
            email.Body = "Any Extra Info Can Help Find The Bug.\nPlease find attached file for the bug report above.";
            await EmailManager.ShowComposeNewEmailAsync(email);
        }

        [RelayCommand]
        private void GenerateAndSend(UserEntity file)
        {
            // no selected file leave

            if (FileSelected is null) return;

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
            // Logic to send email
            FileSelected.BlobUrl = sasUrl.ToString();
            OnPropertyChanged(nameof(FileSelected));
            //SendEmailAsync(SelectedUser.Email, sasUri.ToString());
        }


    }


    public class EmailUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }


}
