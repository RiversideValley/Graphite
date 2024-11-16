using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Riverside.Graphite.Controls;

public class ImageDownloader
{
	public async Task<string> SaveGridAsImageAsync(Grid gridToSave, string imageName, string customFolderPath)
	{
		try
		{
			RenderTargetBitmap renderTargetBitmap = new();
			await renderTargetBitmap.RenderAsync(gridToSave);

			StorageFolder customFolder = await StorageFolder.GetFolderFromPathAsync(customFolderPath);
			StorageFile imageFile = await customFolder.CreateFileAsync(imageName, CreationCollisionOption.GenerateUniqueName);

			using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
			{
				BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);

				IBuffer buffer = await renderTargetBitmap.GetPixelsAsync();
				byte[] pixels = buffer.ToArray();

				encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)renderTargetBitmap.PixelWidth, (uint)renderTargetBitmap.PixelHeight, 96, 96, pixels);
				await encoder.FlushAsync();
			}

			return imageFile.Path;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"{ex.Message}");
		}

		return null;
	}
}