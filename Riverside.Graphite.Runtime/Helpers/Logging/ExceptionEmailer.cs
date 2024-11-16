using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Riverside.Graphite.Runtime.Helpers.Logging;
public static class ExceptionEmailer
{
	public static async Task LogException(Exception ex, XamlRoot root)
	{
		try
		{
			StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("firebrowserwinui.flog", CreationCollisionOption.OpenIfExists);

			using (StreamWriter writer = new(await file.OpenStreamForWriteAsync()))
			{
				writer.WriteLine("Exception Occurred at " + DateTime.Now.ToString());
				writer.WriteLine("Message: " + ex.Message);
				writer.WriteLine("Stack Trace: " + ex.StackTrace);
				writer.WriteLine("Inner Trace: " + ex.InnerException);
				writer.WriteLine("Source Trace: " + ex.Source);
				writer.WriteLine("-------------------------------------------------------------");
			}

			ContentDialog dialog = new()
			{
				Title = "Error",
				Content = $"The app encountered an error. Would you like to send the bug report to the developers?\n{ex.Message}",
				PrimaryButtonText = "Yes",
				CloseButtonText = "No",
				XamlRoot = root
			};

			dialog.PrimaryButtonClick += async (sender, args) =>
			{
				EmailMessage email = new();
				email.Subject = "Bug Report";
				email.To.Add(new EmailRecipient("firebrowserdevs@gmail.com"));
				email.Body = "Any Extra Info Can Help Find The Bug.\nPlease find attached file for the bug report above.";
				IRandomAccessStreamWithContentType fileStream = await file.OpenReadAsync();
				string contentType = file.ContentType;
				RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromStream(fileStream);
				email.Attachments.Add(new EmailAttachment(file.Name, streamRef, contentType));
				await EmailManager.ShowComposeNewEmailAsync(email);
			};

			_ = await dialog.ShowAsync();
		}
		catch
		{
		}
	}
}