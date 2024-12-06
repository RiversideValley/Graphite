using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Riverside.Graphite.Core.Helper;

namespace Riverside.Graphite.Runtime.Helpers
{
	public delegate Task SaveToFileDelegate(JsonElement newArray);

	public class JsonHelper
	{
		public string FileBeingUsed { get; set; }
		public SaveToFileDelegate SaveToFile { get; set; }

		public JsonHelper(string fileBeingUsed)
		{
			FileBeingUsed = fileBeingUsed;
			SaveToFile = SaveJsonElementAsync;
		}

		protected internal async Task SaveJsonElementAsync(JsonElement newArray)
		{
			AsyncLockObject lockObject = new();

			if (!File.Exists(FileBeingUsed))
			{
				_ = File.Create(FileBeingUsed);
			}

			using (await lockObject.LockAsync())
			{
				List<JsonElement> existingArrays = new();

				if (File.Exists(FileBeingUsed))
				{
					string existingContent = await File.ReadAllTextAsync(FileBeingUsed);

					if (!string.IsNullOrWhiteSpace(existingContent))
					{
						using JsonDocument existingDoc = JsonDocument.Parse(existingContent);
						JsonElement existingEntries = existingDoc.RootElement;

						foreach (JsonElement entry in existingEntries.EnumerateArray())
						{
							existingArrays.Add(entry);
						}
					}
				}

				bool isDuplicate = existingArrays.Any(existingArray => JsonElementEquals(existingArray, newArray));

				if (isDuplicate)
				{
					Console.WriteLine("Duplicate entry detected, skipping write.");
				}
				else
				{
					existingArrays.Add(newArray);
					string newContent = JsonSerializer.Serialize(existingArrays, new JsonSerializerOptions { WriteIndented = true });
					await File.WriteAllTextAsync(FileBeingUsed, newContent);
					Console.WriteLine("JsonElement saved to file.");
				}
				await Task.Delay(360);
			}
		}

		private bool JsonElementEquals(JsonElement left, JsonElement right)
		{
			return JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right);
		}
	}
}