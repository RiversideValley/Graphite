using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Navigation;

public class FireTxtReader
{
	private const int BufferSize = (int)(1.5 * 1024 * 1024);

	public async Task<string> ReadTextFile(StorageFile file)
	{
		try
		{
			using Stream stream = await file.OpenStreamForReadAsync();
			using StreamReader reader = new(stream, Encoding.UTF8, true, BufferSize);
			return await reader.ReadToEndAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error reading file: {ex.Message}");
			return string.Empty;
		}
	}
}