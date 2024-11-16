
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services;
public class UpdateService
{
	// URL of the JSON file containing the server versions
	private readonly string jsonUrl = "https://dl.dropboxusercontent.com/scl/fi/h3l38sy7ng18qylbt5dkb/data.json?rlkey=7b7yg46783ftm4955vljnx36b&st=34troqm1&dl=0";

	// Local DLL file names
	private readonly string[] localFileNames = {
	"Riverside.Graphite.Adblock.dll",
	"Riverside.Graphite.Services.BarcodeHost.dll",
	"Riverside.Graphite.Navigation.dll",
	"Riverside.Graphite.Core.dll",
	"Riverside.Graphite.Extensions.dll",
	"Riverside.Graphite.Data.Favorites.dll",
	"Riverside.Graphite.Data.Core.dll",
	"Riverside.Graphite.Data.dll",
	"Riverside.Graphite.Runtime.dll",
	"Riverside.Graphite.IdentityClient.dll",
	"Riverside.Graphite.Assets.dll"
};

	public async Task CheckUpdateAsync()
	{
		try
		{
			// Download the JSON file from the server asynchronously
			using HttpClient client = new();
			string jsonContent = await client.GetStringAsync(jsonUrl);
			Debug.WriteLine("JSON content downloaded successfully.");

			// Parse the JSON data
			Dictionary<string, string> serverData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
			Debug.WriteLine("JSON content parsed successfully.");

			// Get local DLL files in the application folder
			string[] dllFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

			// List to store files that need to be patched
			List<string> filesToPatch = await CompareVersionsAsync(serverData, dllFiles);

			// Write the names of DLLs to be patched into the patch.core file if there are updates
			if (filesToPatch.Count != 0)
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
			_ = Parallel.ForEach(serverVersions, (serverVersion) =>
			{
				string dllFileName = serverVersion.Key + ".dll";
				if (localFileNames.Contains(dllFileName, StringComparer.OrdinalIgnoreCase))
				{
					string localDllPath = localFiles.FirstOrDefault(f => Path.GetFileName(f).Equals(dllFileName, StringComparison.OrdinalIgnoreCase));

					if (localDllPath != null)
					{
						// Get version of the local DLL file
						FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(localDllPath);
						Version localVersion = new(versionInfo.FileVersion);

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
