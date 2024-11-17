using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Helpers
{
	public static class LoadFileHelper
	{
		private const int BufferSize = (int)(1.5 * 1024 * 1024);

		public static async Task<string> LoadFileAsync(Uri fileName, bool errorsSuppressed = true)
		{
			try
			{
				StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(fileName);
				return await ReadTextFile(file);
			}
			catch (FileNotFoundException ex) when (HandleFileNotFoundException(ex))
			{
				ExceptionLogger.LogException(ex);

				if (errorsSuppressed is false)
				{
					throw;
				}
				else
				{
					return string.Empty;
				}
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);

				if (errorsSuppressed is false)
				{
					throw;
				}
				else
				{
					return string.Empty;
				}
			}
		}

		private static async Task<string> ReadTextFile(StorageFile file)
		{
			try
			{
				using Stream stream = await file.OpenStreamForReadAsync();
				using StreamReader reader = new(stream, Encoding.UTF8, true, BufferSize);
				return await reader.ReadToEndAsync();
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				return string.Empty;
			}
		}

		private static bool HandleFileNotFoundException(FileNotFoundException ex)
		{
			ExceptionLogger.LogException(ex);
			return true; // Return true to suppress the exception
		}
	}
}
