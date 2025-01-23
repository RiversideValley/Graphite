using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Data.Core.Models;

public class DataImporter
{
    private readonly DbContext _context;

    public DataImporter(DbContext context)
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
