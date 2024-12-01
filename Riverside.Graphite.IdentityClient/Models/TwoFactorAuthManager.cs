using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Riverside.Graphite.IdentityClient.Models
{
	public class TwoFactorAuthManager
	{
		private readonly string _filePath;
		public ObservableCollection<TwoFactorAuthItem> Items { get; private set; }

		public TwoFactorAuthManager(string filePath)
		{
			_filePath = filePath;
			Items = new ObservableCollection<TwoFactorAuthItem>();
		}

		public async Task SaveAsync()
		{
			try
			{
				string directoryPath = Path.GetDirectoryName(_filePath);
				if (!Directory.Exists(directoryPath))
				{
					Directory.CreateDirectory(directoryPath);
				}

				JsonSerializerOptions options = new() { WriteIndented = true };
				string jsonString = JsonSerializer.Serialize(Items, options);
				await File.WriteAllTextAsync(_filePath, jsonString);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error saving 2FA data: {ex.Message}");
			}
		}

		public async Task LoadAsync()
		{
			try
			{
				if (File.Exists(_filePath))
				{
					string jsonString = await File.ReadAllTextAsync(_filePath);
					var loadedItems = JsonSerializer.Deserialize<ObservableCollection<TwoFactorAuthItem>>(jsonString);
					if (loadedItems != null)
					{
						Items = loadedItems;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading 2FA data: {ex.Message}");
			}
		}

		public void Add(TwoFactorAuthItem item)
		{
			Items.Add(item);
		}

		public void Remove(TwoFactorAuthItem item)
		{
			Items.Remove(item);
		}
	}
}

