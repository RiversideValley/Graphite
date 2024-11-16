using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Riverside.Graphite.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Riverside.Graphite.Runtime.Helpers
{
	public delegate Task SaveToFileDelegate(JArray newArray);

	public class JsonHelper
	{
		public string FileBeingUsed { get; set; }
		public SaveToFileDelegate SaveToFile { get; set; }
		public JsonHelper(string fileBeingUsed)
		{
			FileBeingUsed = fileBeingUsed;
			SaveToFile = SaveJArrayAsync;
		}


		protected internal async Task SaveJArrayAsync(JArray newArray)
		{
			AsyncLockObject lockObject = new();

			if (!File.Exists(FileBeingUsed))
			{
				_ = File.Create(FileBeingUsed);
			}

			using (await lockObject.LockAsync())
			{
				List<JArray> existingArrays = new();

				if (File.Exists(FileBeingUsed))
				{
					string existingContent = await File.ReadAllTextAsync(FileBeingUsed);

					if (!string.IsNullOrWhiteSpace(existingContent))
					{
						JArray existingEntries = JArray.Parse(existingContent);

						foreach (JToken entry in existingEntries)
						{
							existingArrays.Add((JArray)entry);
						}
					}
				}


				bool isDuplicate = existingArrays.Any(existingArray => JToken.DeepEquals(existingArray, newArray));

				if (isDuplicate)
				{
					Console.WriteLine("Duplicate entry detected, skipping write.");
				}
				else
				{
					existingArrays.Add(newArray);
					string newContent = JsonConvert.SerializeObject(existingArrays, Formatting.Indented);
					await File.WriteAllTextAsync(FileBeingUsed, newContent);
					Console.WriteLine("JArray saved to file.");
				}
				await Task.Delay(360);
			}
		}
	}
}
