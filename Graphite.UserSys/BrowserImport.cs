using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Graphite.UserSys;

public class BrowserImport
{
	private readonly string _username;
	private readonly string _userDataPath;

	public BrowserImport(string username)
	{
		_username = username;
		_userDataPath = Path.Combine(UserManager.GraphiteDataPath, username);
	}

	public async Task ImportFromEdgeAsync()
	{
		string edgeDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			@"Microsoft\Edge\User Data\Default");

		await ImportHistoryAsync(edgeDataPath, "Edge");
		await ImportDownloadsAsync(edgeDataPath, "Edge");
		await ImportFavoritesAsync(edgeDataPath, "Edge");
	}

	public async Task ImportFromChromeAsync()
	{
		string chromeDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			@"Google\Chrome\User Data\Default");

		await ImportHistoryAsync(chromeDataPath, "Chrome");
		await ImportDownloadsAsync(chromeDataPath, "Chrome");
		await ImportFavoritesAsync(chromeDataPath, "Chrome");
	}

	public async Task ImportFromFirefoxAsync()
	{
		string firefoxDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			@"Mozilla\Firefox\Profiles");

		// Firefox uses a random profile name, so we need to find it
		string[] profiles = Directory.GetDirectories(firefoxDataPath);
		if (profiles.Length > 0)
		{
			await ImportHistoryAsync(profiles[0], "Firefox");
			await ImportDownloadsAsync(profiles[0], "Firefox");
			await ImportFavoritesAsync(profiles[0], "Firefox");
		}
	}

	public async Task ImportFromArcAsync()
	{
		string arcDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			@"Arc\User Data\Default");

		await ImportHistoryAsync(arcDataPath, "Arc");
		await ImportDownloadsAsync(arcDataPath, "Arc");
		await ImportFavoritesAsync(arcDataPath, "Arc");
	}

	public async Task ImportFromBraveAsync()
	{
		string braveDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			@"BraveSoftware\Brave-Browser\User Data\Default");

		await ImportHistoryAsync(braveDataPath, "Brave");
		await ImportDownloadsAsync(braveDataPath, "Brave");
		await ImportFavoritesAsync(braveDataPath, "Brave");
	}

	private async Task ImportHistoryAsync(string browserDataPath, string browserName)
	{
		string historyDbPath = Path.Combine(browserDataPath, "History");
		string graphiteHistoryDbPath = Path.Combine(_userDataPath, "Database", "history.db");

		if (!File.Exists(historyDbPath))
		{
			Console.WriteLine($"{browserName} history database not found.");
			return;
		}

		try
		{
			await using var sourceConnection = new SqliteConnection($"Data Source={historyDbPath};Mode=ReadOnly");
			await sourceConnection.OpenAsync();

			await using var destConnection = new SqliteConnection($"Data Source={graphiteHistoryDbPath}");
			await destConnection.OpenAsync();

			var command = sourceConnection.CreateCommand();
			command.CommandText = @"
                    SELECT url, title, last_visit_time 
                    FROM urls 
                    ORDER BY last_visit_time DESC 
                    LIMIT 1000";  // Limit to recent 1000 entries

			await using var reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var insertCommand = destConnection.CreateCommand();
				insertCommand.CommandText = @"
                        INSERT OR IGNORE INTO History (Url, Title, VisitTime, Source)
                        VALUES ($url, $title, $visitTime, $source)";

				insertCommand.Parameters.AddWithValue("$url", reader.GetString(0));
				insertCommand.Parameters.AddWithValue("$title", reader.GetString(1));
				insertCommand.Parameters.AddWithValue("$visitTime", reader.GetInt64(2));
				insertCommand.Parameters.AddWithValue("$source", browserName);

				await insertCommand.ExecuteNonQueryAsync();
			}

			Console.WriteLine($"Imported history from {browserName}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error importing {browserName} history: {ex.Message}");
		}
	}

	private async Task ImportDownloadsAsync(string browserDataPath, string browserName)
	{
		string downloadsDbPath = Path.Combine(browserDataPath, "History");  // Downloads are typically in the History database
		string graphiteDownloadsDbPath = Path.Combine(_userDataPath, "Database", "downloads.db");

		if (!File.Exists(downloadsDbPath))
		{
			Console.WriteLine($"{browserName} downloads database not found.");
			return;
		}

		try
		{
			await using var sourceConnection = new SqliteConnection($"Data Source={downloadsDbPath};Mode=ReadOnly");
			await sourceConnection.OpenAsync();

			await using var destConnection = new SqliteConnection($"Data Source={graphiteDownloadsDbPath}");
			await destConnection.OpenAsync();

			var command = sourceConnection.CreateCommand();
			command.CommandText = @"
                    SELECT target_path, tab_url, start_time, received_bytes, total_bytes
                    FROM downloads
                    ORDER BY start_time DESC
                    LIMIT 1000";  // Limit to recent 1000 entries

			await using var reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var insertCommand = destConnection.CreateCommand();
				insertCommand.CommandText = @"
                        INSERT OR IGNORE INTO Downloads (FilePath, SourceUrl, StartTime, ReceivedBytes, TotalBytes, Source)
                        VALUES ($filePath, $sourceUrl, $startTime, $receivedBytes, $totalBytes, $source)";

				insertCommand.Parameters.AddWithValue("$filePath", reader.GetString(0));
				insertCommand.Parameters.AddWithValue("$sourceUrl", reader.GetString(1));
				insertCommand.Parameters.AddWithValue("$startTime", reader.GetInt64(2));
				insertCommand.Parameters.AddWithValue("$receivedBytes", reader.GetInt64(3));
				insertCommand.Parameters.AddWithValue("$totalBytes", reader.GetInt64(4));
				insertCommand.Parameters.AddWithValue("$source", browserName);

				await insertCommand.ExecuteNonQueryAsync();
			}

			Console.WriteLine($"Imported downloads from {browserName}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error importing {browserName} downloads: {ex.Message}");
		}
	}

	private async Task ImportFavoritesAsync(string browserDataPath, string browserName)
	{
		string favoritesPath = browserName.ToLower() switch
		{
			"edge" or "chrome" or "brave" => Path.Combine(browserDataPath, "Bookmarks"),
			"firefox" => Path.Combine(browserDataPath, "places.sqlite"),
			"arc" => Path.Combine(browserDataPath, "Bookmarks"),
			_ => throw new ArgumentException("Unsupported browser", nameof(browserName))
		};

		string graphiteFavoritesDbPath = Path.Combine(_userDataPath, "Database", "favorites.db");

		if (!File.Exists(favoritesPath))
		{
			Console.WriteLine($"{browserName} favorites not found.");
			return;
		}

		try
		{
			if (browserName.ToLower() == "firefox")
			{
				await ImportFirefoxFavoritesAsync(favoritesPath, graphiteFavoritesDbPath, browserName);
			}
			else
			{
				await ImportChromiumBasedFavoritesAsync(favoritesPath, graphiteFavoritesDbPath, browserName);
			}

			Console.WriteLine($"Imported favorites from {browserName}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error importing {browserName} favorites: {ex.Message}");
		}
	}

	private async Task ImportFirefoxFavoritesAsync(string sourcePath, string destPath, string browserName)
	{
		await using var sourceConnection = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
		await sourceConnection.OpenAsync();

		await using var destConnection = new SqliteConnection($"Data Source={destPath}");
		await destConnection.OpenAsync();

		var command = sourceConnection.CreateCommand();
		command.CommandText = @"
                SELECT moz_bookmarks.title, moz_places.url
                FROM moz_bookmarks
                JOIN moz_places ON moz_bookmarks.fk = moz_places.id
                WHERE moz_bookmarks.type = 1";

		await using var reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			var insertCommand = destConnection.CreateCommand();
			insertCommand.CommandText = @"
                    INSERT OR IGNORE INTO Favorites (Title, Url, Source)
                    VALUES ($title, $url, $source)";

			insertCommand.Parameters.AddWithValue("$title", reader.GetString(0));
			insertCommand.Parameters.AddWithValue("$url", reader.GetString(1));
			insertCommand.Parameters.AddWithValue("$source", browserName);

			await insertCommand.ExecuteNonQueryAsync();
		}
	}

	private async Task ImportChromiumBasedFavoritesAsync(string sourcePath, string destPath, string browserName)
	{
		string json = await File.ReadAllTextAsync(sourcePath);
		using JsonDocument doc = JsonDocument.Parse(json);
		var roots = doc.RootElement.GetProperty("roots");

		await using var destConnection = new SqliteConnection($"Data Source={destPath}");
		await destConnection.OpenAsync();

		await ImportBookmarkFolder(roots.GetProperty("bookmark_bar"), destConnection, browserName);
		await ImportBookmarkFolder(roots.GetProperty("other"), destConnection, browserName);
	}

	private async Task ImportBookmarkFolder(JsonElement folder, SqliteConnection destConnection, string browserName)
	{
		if (folder.TryGetProperty("children", out JsonElement children))
		{
			foreach (var child in children.EnumerateArray())
			{
				if (child.GetProperty("type").GetString() == "url")
				{
					var insertCommand = destConnection.CreateCommand();
					insertCommand.CommandText = @"
                            INSERT OR IGNORE INTO Favorites (Title, Url, Source)
                            VALUES ($title, $url, $source)";

					insertCommand.Parameters.AddWithValue("$title", child.GetProperty("name").GetString());
					insertCommand.Parameters.AddWithValue("$url", child.GetProperty("url").GetString());
					insertCommand.Parameters.AddWithValue("$source", browserName);

					await insertCommand.ExecuteNonQueryAsync();
				}
				else if (child.GetProperty("type").GetString() == "folder")
				{
					await ImportBookmarkFolder(child, destConnection, browserName);
				}
			}
		}
	}
}
