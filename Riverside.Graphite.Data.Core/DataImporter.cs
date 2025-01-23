using Riverside.Graphite.Data.Core;
using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class DataImporter
{
    private readonly HistoryContext _context;

    public DataImporter(HistoryContext context)
    {
        _context = context;
    }

    public async Task ImportCollectionsAsync(string filePath)
    {
        var jsonData = await File.ReadAllTextAsync(filePath);
        var collections = JsonSerializer.Deserialize<List<Collection>>(jsonData);

        if (collections != null)
        {
            _context.Collections.AddRange(collections);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ImportCollectionNamesAsync(string filePath)
    {
        var jsonData = await File.ReadAllTextAsync(filePath);
        var collectionNames = JsonSerializer.Deserialize<List<CollectionName>>(jsonData);

        if (collectionNames != null)
        {
            _context.CollectionNames.AddRange(collectionNames);
            await _context.SaveChangesAsync();
        }
    }
}
