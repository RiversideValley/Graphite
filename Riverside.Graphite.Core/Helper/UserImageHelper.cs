using Riverside.Graphite.Core.Helper.Logging;
using Riverside.Graphite.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Core.Helper
{
	public class UserImageHelper
	{
		public static async Task<MemoryStream> GetImageStreamAsync(UserImageItem selectedImage)
		{
			try
			{

				var memoryStream = new MemoryStream();
				var imageUri = new Uri(selectedImage.ImagePath);
				var imageFile = await StorageFile.GetFileFromApplicationUriAsync(imageUri);

			// Open the file as a stream
			using (var imageStream = await imageFile.OpenReadAsync())
			{
				await imageStream.AsStreamForRead().CopyToAsync(memoryStream);
				memoryStream.Position = 0; // Reset the position to the beginning of the stream
			}
				return memoryStream;
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(new Exception("Failed to get image stream",ex));
				return new MemoryStream(); // Return an empty stream
			}
		}
	}
}
