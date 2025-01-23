using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core;
using System.Threading.Tasks;

public class DatabaseImportProgram
{
    public async Task Main()
    {
        
        var context = new HistoryContext(AuthService.CurrentUser?.Username);
        var importer = new DataImporter(context);

        await importer.ImportCollectionsAsync("SampleData/Collections.json");
        await importer.ImportCollectionNamesAsync("SampleData/CollectionNames.json");
    }
}
