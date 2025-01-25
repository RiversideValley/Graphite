using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core; 
public class DatabaseImportProgram
{
	public static string Get_Appx_AssemblyDirectory(Assembly assembly)
	{
		string assemblyLocation = assembly.Location;
		string directoryPath = Path.GetDirectoryName(assemblyLocation);

		return directoryPath ?? throw new DirectoryNotFoundException("Publish directory not found");

	}
	public static async Task Main(string[] args)
    {
        var context = new HistoryContext(AuthService.CurrentUser?.Username);
        var importer = new DataImporter(context);
		string publishDirectory = Get_Appx_AssemblyDirectory(typeof(Riverside.Graphite.Data.Core.DatabaseImportProgram).Assembly);

		await importer.ImportCollectionNamesAsync(Path.Combine(publishDirectory, "SampleData\\CollectionNames.json"));
		await importer.ImportCollectionsAsync(Path.Combine(publishDirectory, "SampleData\\Collections.json"));
        
    }
}
