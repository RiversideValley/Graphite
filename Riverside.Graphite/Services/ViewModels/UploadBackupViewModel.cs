using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Behaviors;
using Riverside.Graphite.Core;
using Riverside.Graphite.Pages.Patch;
using Riverside.Graphite.Services.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.ViewModels
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
			Microsoft.Identity.Client.IAccount user = AppService.MsalService.GetUserAccountAsync().GetAwaiter().GetResult();

			if (user is not null)
			{
				SelectedUser = new EmailUser { Name = user.Username, Email = user.Username };
				string connString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] as string;
				AzBackup = new AzBackupService(connString, "storelean", "firebackups", AuthService.CurrentUser ?? new() { Id = Guid.NewGuid(), Username = "Admin", IsFirstLaunch = false });

				System.Collections.Generic.List<UserEntity> list = AzBackup.GetUploadFileByUser(user.Username);
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
				_ = (UpLoadBackup.Instance?.NotificationQueue.Show("Please select a file from the list", 3000, "Backups"));
				return;
			}

			AzBackupService.ResponseAZFILE result = await AzBackup.DownloadBackupFile(FileSelected.BlobName);
			if (result is null)
			{
				return;
			}

			Notification note = new()
			{
				Title = "Backup Manager \n",
				Message = $"Your backup file has been downloaded:\n{FileSelected.Timestamp.Value.ToLocalTime()} Restore Point !",
				Severity = InfoBarSeverity.Informational,
				IsIconVisible = true,
				Duration = TimeSpan.FromSeconds(3)
			};
			_ = (UpLoadBackup.Instance?.NotificationQueue.Show(note));

		}

		[RelayCommand]
		private async Task BackUpCore()
		{

			BackUpDialog dlg = new();
			dlg.XamlRoot = UpLoadBackup.Instance?.GridMainUploadBackup.XamlRoot;
			_ = await dlg.ShowAsync();

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
				_ = (UpLoadBackup.Instance?.NotificationQueue.Show("Please select a file!", 3000, "Backups"));
				return;
			}

			// get ref to file on Azure Blob Storage

			string connString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AzureStorageConnectionString"] as string;
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connString);
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			CloudBlobContainer container = blobClient.GetContainerReference("firebackups");
			CloudBlockBlob blob = container.GetBlockBlobReference(FileSelected.BlobName);

			string sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
			{
				Permissions = SharedAccessBlobPermissions.Read,
				SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1) // Token valid for 1 hour
			});

			string sasUrl = blob.Uri.AbsoluteUri + sasToken;
			_ = sasUrl.Append('/');
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
