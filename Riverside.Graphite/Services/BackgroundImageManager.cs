using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using Riverside.Graphite.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Text.Json;

namespace Riverside.Graphite.Services
{
	public class BackgroundImageManager
	{
		private static readonly string CacheFolder = Path.Combine(UserDataManager.CoreFolderPath, "Users", AuthService.CurrentUser.Username);
		private static readonly string CacheFilePrefix = "cachedaily";
		private static ImageBrush cachedImageBrush = null;

		public static async Task<ImageBrush> GetFeaturedBackgroundAsync()
		{
			string cachedImagePath = GetCachedImagePath();

			if (await IsCachedImageValidAsync(cachedImagePath))
			{
				return await CreateImageBrushFromFileAsync(cachedImagePath);
			}

			SocketsHttpHandler handler = new()
			{
				EnableMultipleHttp2Connections = true,
				SslOptions = new SslClientAuthenticationOptions
				{
					ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
				}
			};

			using HttpClient client = new(handler);

			try
			{
				string request = await client.GetStringAsync(new Uri("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1"));

				ImageRoot images = JsonSerializer.Deserialize<ImageRoot>(request);
				Uri imageUrl = new("https://bing.com" + images.images[0].url);

				// Download and save the image
				byte[] imageData = await client.GetByteArrayAsync(imageUrl);
				await File.WriteAllBytesAsync(cachedImagePath, imageData);

				cachedImageBrush = await CreateImageBrushFromFileAsync(cachedImagePath);
				return cachedImageBrush;
			}
			catch (HttpRequestException httpEx)
			{
				Console.WriteLine($"HTTP request error: {httpEx.Message}");
			}
			catch (JsonException jsonEx)
			{
				Console.WriteLine($"Error parsing JSON: {jsonEx.Message}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}

			return null;
		}

		private static string GetCachedImagePath()
		{
			string date = DateTime.Now.ToString("yyyyMMdd");
			string jpgPath = Path.Combine(CacheFolder, $"{CacheFilePrefix}{date}.jpg");
			string pngPath = Path.Combine(CacheFolder, $"{CacheFilePrefix}{date}.png");

			return File.Exists(jpgPath) ? jpgPath : pngPath;
		}

		private static async Task<bool> IsCachedImageValidAsync(string path)
		{
			if (!File.Exists(path))
				return false;

			FileInfo fileInfo = new FileInfo(path);
			return (DateTime.Now - fileInfo.CreationTime).TotalHours < 24;
		}

		private static async Task<ImageBrush> CreateImageBrushFromFileAsync(string path)
		{
			StorageFile file = await StorageFile.GetFileFromPathAsync(path);
			using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);

			BitmapImage btpImg = new BitmapImage();
			await btpImg.SetSourceAsync(stream);

			return new ImageBrush()
			{
				ImageSource = btpImg,
				Stretch = Stretch.UniformToFill
			};
		}

		public class ImageRoot
		{
			public List<Image> images { get; set; }
		}

		public class Image
		{
			public string url { get; set; }
		}
	}
}

