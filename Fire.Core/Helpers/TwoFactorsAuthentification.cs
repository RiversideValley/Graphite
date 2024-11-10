using Fire.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fire.Core.Helpers
{
	public static class TwoFactorsAuthentification
	{
		public static ObservableCollection<TwoFactorAuthItem> Items { get; private set; } = new();

		public static void Save()
		{
			try
			{
				var options = new JsonSerializerOptions { WriteIndented = true };
				string jsonString = JsonSerializer.Serialize(Items, options);
				byte[] encryptedData = EncryptionHelpers.ProtectString(jsonString);

				if (encryptedData != null)
				{
					File.WriteAllBytes(Models.Data.TotpFilePath, encryptedData);
				}
				else
				{
					Console.WriteLine("Failed to encrypt data during save.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error saving 2FA data: {ex.Message}");
			}
		}

		public static async Task<ObservableCollection<TwoFactorAuthItem>> Load()
		{
			try
			{
				if (File.Exists(Models.Data.TotpFilePath))
				{
					byte[] encryptedJsonString = File.ReadAllBytes(Models.Data.TotpFilePath);
					string jsonString = EncryptionHelpers.UnprotectToString(encryptedJsonString);

					if (!string.IsNullOrEmpty(jsonString))
					{
						Items = JsonSerializer.Deserialize<ObservableCollection<TwoFactorAuthItem>>(jsonString) ?? new ObservableCollection<TwoFactorAuthItem>();
					}
					else
					{
						Items = new ObservableCollection<TwoFactorAuthItem>();
					}
				}
				else
				{
					Items = new ObservableCollection<TwoFactorAuthItem>();
				}

				Items.CollectionChanged += (_, _) => Save();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading 2FA data: {ex.Message}");
				Items = new ObservableCollection<TwoFactorAuthItem>();
			}

			await Task.CompletedTask;
			return Items;
		}

		public static async Task Repair()
		{
			try
			{
				if (File.Exists(Models.Data.TotpFilePath))
				{
					byte[] encryptedJsonString = File.ReadAllBytes(Models.Data.TotpFilePath);
					string jsonString = EncryptionHelpers.UnprotectToString(encryptedJsonString);

					if (string.IsNullOrEmpty(jsonString))
					{
						Console.WriteLine("2FA data is empty or cannot be decrypted. Creating default file.");
						CreateDefaultFile();
					}
					else
					{
						try
						{
							var items = JsonSerializer.Deserialize<ObservableCollection<TwoFactorAuthItem>>(jsonString);

							if (items == null)
							{
								Console.WriteLine("Deserialization failed. Creating default file.");
								CreateDefaultFile();
							}
							else
							{
								Items = items;
								Items.CollectionChanged += (_, _) => Save();
							}
						}
						catch (JsonException)
						{
							Console.WriteLine("Deserialization error. Creating default file.");
							CreateDefaultFile();
						}
					}
				}
				else
				{
					Console.WriteLine("2FA file does not exist. Creating default file.");
					CreateDefaultFile();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error repairing 2FA data: {ex.Message}");
				CreateDefaultFile();
			}

			await Task.CompletedTask;
		}

		private static void CreateDefaultFile()
		{
			Items = new ObservableCollection<TwoFactorAuthItem>();
			Items.CollectionChanged += (_, _) => Save();
			Save();
		}
	}
}
