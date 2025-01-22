using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Text.Json;
using Riverside.Graphite.Runtime.Models;
using Riverside.Graphite.Core;
using Settings = Riverside.Graphite.Runtime.Models.Settings;

namespace Riverside.Graphite.Services
{
	#region DependableClasses
	public class ImageRoot
	{
		public List<Image> Images { get; set; }
	}

	public class Image
	{
		public string Url { get; set; }
	}
	#endregion
	public class BackgroundManager
	{
		private  readonly string CacheFolder = Path.Combine(UserDataManager.CoreFolderPath, "Users", AuthService.CurrentUser.Username);
		private  readonly string CacheFilePrefix = "cachedaily";
		private  ImageBrush cachedImageBrush = null;

		public  async Task<ImageBrush> GetFeaturedBackgroundAsync()
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
				Uri imageUrl = new("https://bing.com" + images.Images[0].Url);

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

		private  string GetCachedImagePath()
		{
			string date = DateTime.Now.ToString("yyyyMMdd");
			string jpgPath = Path.Combine(CacheFolder, $"{CacheFilePrefix}{date}.jpg");
			string pngPath = Path.Combine(CacheFolder, $"{CacheFilePrefix}{date}.png");

			return File.Exists(jpgPath) ? jpgPath : pngPath;
		}

		private  async Task<bool> IsCachedImageValidAsync(string path)
		{
			if (!File.Exists(path))
				return false;

			FileInfo fileInfo = new FileInfo(path);
			return (DateTime.Now - fileInfo.CreationTime).TotalHours < 24;
		}

		private  async Task<ImageBrush> CreateImageBrushFromFileAsync(string path)
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

		public async Task<Brush> GetGridBackgroundAsync(Settings.NewTabBackground backgroundType, Riverside.Graphite.Core.Settings userSettings)
		{
			switch (backgroundType)
			{
				case Settings.NewTabBackground.None:
					return new SolidColorBrush(Colors.Transparent);

				case Settings.NewTabBackground.Costum:
					string colorString = userSettings.ColorBackground?.ToString() ?? "#000000";

					Color color = colorString == "#000000" ?
					 Colors.Transparent :
					 (Color)XamlBindingHelper.ConvertValue(typeof(Color), colorString);
					return new SolidColorBrush(color);

				case Settings.NewTabBackground.Featured:
					return await GetFeaturedBackgroundAsync();

				default:
					return new SolidColorBrush(Colors.Transparent);
			}
		}
	}
	
}
