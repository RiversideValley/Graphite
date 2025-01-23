public class DatabaseImportProgram
{
    public static async Task Main(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=sample.db")
            .Options;

        using var context = new ApplicationDbContext(options);
        var importer = new DataImporter(context);

        await importer.ImportCollectionsAsync("SampleData/Collections.json");
        await importer.ImportCollectionNamesAsync("SampleData/CollectionNames.json");
    }
}
