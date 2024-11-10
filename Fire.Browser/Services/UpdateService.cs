
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services;
public class UpdateService
{
    // URL of the JSON file containing the server versions
    private readonly string jsonUrl = "https://dl.dropboxusercontent.com/scl/fi/h3l38sy7ng18qylbt5dkb/data.json?rlkey=7b7yg46783ftm4955vljnx36b&st=34troqm1&dl=0";

    // Local DLL file names
    private readonly string[] localFileNames = {
    "FireBrowserWinUi3AdBlockCore.dll",
    "FireBrowserWinUi3Setup.dll",
    "FireBrowserWinUi3QrCore.dll",
    "FireBrowserWinUi3Navigator.dll",
    "FireBrowserWinUi3MultiCore.dll",
    "FireBrowserWinUi3Modules.dll",
    "FireBrowserWinUi3Favorites.dll",
    "FireBrowserWinUi3Exceptions.dll",
    "FireBrowserWinUi3DataCore.dll",
    "FireBrowserWinUi3Database.dll",
    "FireBrowserWinUi3Core.dll",
    "FireBrowserWinUi3Auth.dll",
    "FireBrowserWinUi3AuthCore.dll",
    "FireBrowserWinUi3Assets.dll"
};

    public async Task CheckUpdateAsync()
    {
        try
        {
            // Download the JSON file from the server asynchronously
            using HttpClient client = new HttpClient();
            string jsonContent = await client.GetStringAsync(jsonUrl);
            Debug.WriteLine("JSON content downloaded successfully.");

            // Parse the JSON data
            var serverData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            Debug.WriteLine("JSON content parsed successfully.");

            // Get local DLL files in the application folder
            string[] dllFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

            // List to store files that need to be patched
            var filesToPatch = await CompareVersionsAsync(serverData, dllFiles);

            // Write the names of DLLs to be patched into the patch.core file if there are updates
            if (filesToPatch.Any())
            {
                string patchCoreFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "patch.core");
                await File.WriteAllLinesAsync(patchCoreFilePath, filesToPatch);
                Debug.WriteLine("Patch file created successfully.");
            }
            else
            {
                Debug.WriteLine("No updates required.");
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<List<string>> CompareVersionsAsync(Dictionary<string, string> serverVersions, string[] localFiles)
    {
        List<string> filesToPatch = new();

        await Task.Run(() =>
        {
            Parallel.ForEach(serverVersions, (serverVersion) =>
            {
                string dllFileName = serverVersion.Key + ".dll";
                if (localFileNames.Contains(dllFileName, StringComparer.OrdinalIgnoreCase))
                {
                    string localDllPath = localFiles.FirstOrDefault(f => Path.GetFileName(f).Equals(dllFileName, StringComparison.OrdinalIgnoreCase));

                    if (localDllPath != null)
                    {
                        // Get version of the local DLL file
                        var versionInfo = FileVersionInfo.GetVersionInfo(localDllPath);
                        Version localVersion = new Version(versionInfo.FileVersion);

                        // Compare with server version
                        if (Version.TryParse(serverVersion.Value, out Version serverParsedVersion) && serverParsedVersion > localVersion)
                        {
                            filesToPatch.Add(dllFileName);
                        }
                    }
                }
            });
        });

        return filesToPatch;
    }
}
