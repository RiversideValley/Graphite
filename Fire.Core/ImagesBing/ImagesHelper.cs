using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Fire.Core.ImagesBing;
public class ImagesHelper
{
    public async Task AppendToJsonAsync(string storedDbPath, StoredImages imageData)
    {
        try
        {
            string jsonData = File.ReadAllText(storedDbPath);

            List<StoredImages> storedImages = System.Text.Json.JsonSerializer.Deserialize<List<StoredImages>>(jsonData);
            storedImages.Add(imageData);

            string updatedJsonData = System.Text.Json.JsonSerializer.Serialize(storedImages);

            File.WriteAllText(storedDbPath, updatedJsonData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
            // Handle the exception as needed: log, display to the user, etc.
        }
        
    }
}