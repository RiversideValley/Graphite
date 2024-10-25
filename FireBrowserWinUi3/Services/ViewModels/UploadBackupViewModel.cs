using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireBrowserWinUi3.Services.Models;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3MultiCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Email;
using Windows.Storage.Streams;

namespace FireBrowserWinUi3.Services.ViewModels
{
    public partial class UploadBackupViewModel : ObservableRecipient
    {
        public ObservableCollection<EmailUser> Users { get; set; }
        public ObservableCollection<UserEntity> FilesUpload { get; set; }
        public EmailUser SelectedUser { get; set; }

        [ObservableProperty]
        private UserEntity _FileSelected;
        public UploadBackupViewModel()
        {
            Users = new ObservableCollection<EmailUser>
            {
                new EmailUser { Name = "User1", Email = "user1@example.com" },
                new EmailUser { Name = "User2", Email = "user2@example.com" }
            };

            var connString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] as string;
            var AzBackup = new AzBackupService(connString, "storelean", "firebackups", AuthService.CurrentUser ?? new() { Id = Guid.NewGuid(), Username = "Admin", IsFirstLaunch = false });

            var list = AzBackup.GetUploadFileByUser("fizzledbydizzle@live.com");
            FilesUpload = [.. list];
        }


        [RelayCommand]
        private void SelectFile()
        {

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
        private void GenerateAndSend()
        {
            // Example SAS generation logic
            string blobUri = "https://yourstorageaccount.blob.core.windows.net/yourcontainer/yourfile";
            BlobServiceClient blobServiceClient = new BlobServiceClient("YourConnectionString");
            BlobClient blobClient = new BlobClient(new Uri(blobUri));

            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            // Logic to send email
            SendEmailAsync(SelectedUser.Email, sasUri.ToString());
        }


    }


    public class EmailUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }


}
