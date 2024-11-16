using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text;

namespace Riverside.Graphite.Navigation;

public class TLD
{
    private const int BufferSize = (int)(1.5 * 1024 * 1024);

    public static async Task<string> ReadTextFile(StorageFile file)
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

    public static string KnownDomains { get; set; }

    public static async Task LoadKnownDomainsAsync()
    {
        try
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Navigation/DomainMap.txt"));
            KnownDomains = await ReadTextFile(file);
        }
        catch (FileNotFoundException ex) when (HandleFileNotFoundException(ex))
        {
            Debug.WriteLine("File not found: " + ex.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static bool HandleFileNotFoundException(FileNotFoundException ex)
    {
        ExceptionLogger.LogException(ex);
        return true; // Return true to suppress the exception
    }

    public static string GetTLDfromURL(string url) => url[(url.LastIndexOf(".") + 1)..];
}
