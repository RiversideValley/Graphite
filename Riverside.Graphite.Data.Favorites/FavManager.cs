using Microsoft.Data.Sqlite;
using Riverside.Graphite.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Riverside.Graphite.Data.Favorites;
public class FavManager
{
	private readonly string _favoritesDbPath;

	public FavManager()
	{
		_favoritesDbPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "Favorites.db");
		InitializeDatabase();
	}

	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
	[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
	private void InitializeDatabase()
	{
		// Create the directory if it doesn't exist
		_ = Directory.CreateDirectory(Path.GetDirectoryName(_favoritesDbPath));

		// Create or open the SQLite database file
		using (SqliteConnection connection = new($"Data Source={_favoritesDbPath}"))
		{
			connection.Open();

			// Create the Favorites table if it doesn't exist
			string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Favorites (
                    Title TEXT,
                    Url TEXT,
                    IconUrlPath TEXT
                )";
			SqliteCommand createTableCommand = new(createTableSql, connection);
			_ = createTableCommand.ExecuteNonQuery();
		}

		// Check if the old favorites.json file exists
		string oldFavoritesJsonPath = Path.Combine(Path.GetDirectoryName(_favoritesDbPath), "favorites.json");
		if (File.Exists(oldFavoritesJsonPath))
		{
			// Read the JSON content from the file
			string jsonContent = File.ReadAllText(oldFavoritesJsonPath);

			// Deserialize JSON content into a list of FavItem
			List<FavItem> favoritesList = JsonSerializer.Deserialize<List<FavItem>>(jsonContent);

			// Insert each item into the SQLite database
			using (SqliteConnection connection = new($"Data Source={_favoritesDbPath}"))
			{
				connection.Open();
				foreach (FavItem favItem in favoritesList)
				{
					string insertSql = "INSERT INTO Favorites (Title, Url, IconUrlPath) VALUES (@Title, @Url, @IconUrlPath)";
					SqliteCommand insertCommand = new(insertSql, connection);
					_ = insertCommand.Parameters.AddWithValue("@Title", favItem.Title);
					_ = insertCommand.Parameters.AddWithValue("@Url", favItem.Url);
					_ = insertCommand.Parameters.AddWithValue("@IconUrlPath", favItem.IconUrlPath);
					_ = insertCommand.ExecuteNonQuery();
				}
			}

			// Remove the old favorites.json file
			File.Delete(oldFavoritesJsonPath);
		}
	}

	public void SaveFav(string title, string url)
	{
		using SqliteConnection connection = new($"Data Source={_favoritesDbPath}");
		connection.Open();
		string insertSql = "INSERT INTO Favorites (Title, Url, IconUrlPath) VALUES (@Title, @Url, @IconUrlPath)";
		SqliteCommand insertCommand = new(insertSql, connection);
		_ = insertCommand.Parameters.AddWithValue("@Title", title);
		_ = insertCommand.Parameters.AddWithValue("@Url", url);
		_ = insertCommand.Parameters.AddWithValue("@IconUrlPath", $"https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={url}&size=32");
		_ = insertCommand.ExecuteNonQuery();
	}

	public List<FavItem> LoadFav()
	{
		List<FavItem> favorites = new();
		using (SqliteConnection connection = new($"Data Source={_favoritesDbPath}"))
		{
			connection.Open();
			string selectSql = "SELECT * FROM Favorites";
			SqliteCommand selectCommand = new(selectSql, connection);
			using SqliteDataReader reader = selectCommand.ExecuteReader();
			while (reader.Read())
			{
				FavItem favItem = new()
				{
					Title = reader.GetString(reader.GetOrdinal("Title")),
					Url = reader.GetString(reader.GetOrdinal("Url")),
					IconUrlPath = reader.GetString(reader.GetOrdinal("IconUrlPath"))
				};
				favorites.Add(favItem);
			}
		}
		return favorites;
	}

	public void ClearFavs()
	{
		using SqliteConnection connection = new($"Data Source={_favoritesDbPath}");
		connection.Open();
		string deleteSql = "DELETE FROM Favorites";
		SqliteCommand deleteCommand = new(deleteSql, connection);
		_ = deleteCommand.ExecuteNonQuery();
	}

	public void RemoveFavorite(FavItem selectedItem)
	{
		if (selectedItem == null)
		{
			// Handle the case where the selected item is null (optional)
			Console.WriteLine("Selected item is null.");
			return;
		}

		using SqliteConnection connection = new($"Data Source={_favoritesDbPath}");
		connection.Open();
		string deleteSql = "DELETE FROM Favorites WHERE Title = @Title AND Url = @Url";
		SqliteCommand deleteCommand = new(deleteSql, connection);
		_ = deleteCommand.Parameters.AddWithValue("@Title", selectedItem.Title);
		_ = deleteCommand.Parameters.AddWithValue("@Url", selectedItem.Url);
		_ = deleteCommand.ExecuteNonQuery();
	}

	public void ImportFavoritesFromOtherBrowsers(string browserName)
	{
		_ = new List<FavItem>();
		List<FavItem> importedFavorites;
		string favoritesPath;
		switch (browserName.ToLower())
		{
			case "chrome":
				favoritesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					@"Google\Chrome\User Data\Default\Bookmarks");
				importedFavorites = ImportChromeBookmarks(favoritesPath);
				break;
			case "firefox":
				favoritesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					@"Mozilla\Firefox\Profiles");
				importedFavorites = ImportFirefoxBookmarks(favoritesPath);
				break;
			case "edge":
				favoritesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					@"Microsoft\Edge\User Data\Default\Bookmarks");
				importedFavorites = ImportEdgeBookmarks(favoritesPath);
				break;
			case "arc":
				favoritesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					@"Arc\User Data\Default\Bookmarks");
				importedFavorites = ImportArcBookmarks(favoritesPath);
				break;
			case "brave":
				favoritesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					@"BraveSoftware\Brave-Browser\User Data\Default\Bookmarks");
				importedFavorites = ImportBraveBookmarks(favoritesPath);
				break;
			default:
				throw new ArgumentException("Unsupported browser. Please choose 'Chrome', 'Firefox', 'Edge', or 'Arc'.");
		}

		// Import the favorites into our database
		using SqliteConnection connection = new($"Data Source={_favoritesDbPath}");
		connection.Open();
		foreach (FavItem favItem in importedFavorites)
		{
			string insertSql = "INSERT INTO Favorites (Title, Url, IconUrlPath) VALUES (@Title, @Url, @IconUrlPath)";
			SqliteCommand insertCommand = new(insertSql, connection);
			_ = insertCommand.Parameters.AddWithValue("@Title", favItem.Title);
			_ = insertCommand.Parameters.AddWithValue("@Url", favItem.Url);
			_ = insertCommand.Parameters.AddWithValue("@IconUrlPath", favItem.IconUrlPath);
			_ = insertCommand.ExecuteNonQuery();
		}
	}

	private List<FavItem> ImportBraveBookmarks(string bookmarksPath)
	{
		// Brave is based on Chromium, so it uses the same bookmarks format as Chrome
		return ImportChromeBookmarks(bookmarksPath);
	}

	private List<FavItem> ImportArcBookmarks(string bookmarksPath)
	{
		// Arc is based on Chromium, so it likely uses the same bookmarks format as Chrome
		return ImportChromeBookmarks(bookmarksPath);
	}
	private List<FavItem> ImportChromeBookmarks(string bookmarksPath)
	{
		List<FavItem> favorites = new();

		if (!File.Exists(bookmarksPath))
		{
			Console.WriteLine("Chrome bookmarks file not found.");
			return favorites;
		}

		string json = File.ReadAllText(bookmarksPath);
		using (JsonDocument document = JsonDocument.Parse(json))
		{
			JsonElement root = document.RootElement;
			JsonElement bookmarkBar = root.GetProperty("roots").GetProperty("bookmark_bar");
			ParseChromeBookmarkNode(bookmarkBar, favorites);
		}

		return favorites;
	}

	private void ParseChromeBookmarkNode(JsonElement node, List<FavItem> favorites)
	{
		if (node.TryGetProperty("type", out JsonElement typeElement) && typeElement.GetString() == "url")
		{
			string title = node.GetProperty("name").GetString();
			string url = node.GetProperty("url").GetString();
			favorites.Add(new FavItem
			{
				Title = title,
				Url = url,
				IconUrlPath = $"https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={url}&size=32"
			});
		}
		else if (node.TryGetProperty("children", out JsonElement childrenElement))
		{
			foreach (JsonElement child in childrenElement.EnumerateArray())
			{
				ParseChromeBookmarkNode(child, favorites);
			}
		}
	}

	private List<FavItem> ImportFirefoxBookmarks(string profilesPath)
	{
		List<FavItem> favorites = new();

		if (!Directory.Exists(profilesPath))
		{
			Console.WriteLine("Firefox profiles directory not found.");
			return favorites;
		}

		string[] profiles = Directory.GetDirectories(profilesPath);
		foreach (string profile in profiles)
		{
			string placesPath = Path.Combine(profile, "places.sqlite");
			if (File.Exists(placesPath))
			{
				using SqliteConnection connection = new($"Data Source={placesPath}");
				connection.Open();
				string selectSql = "SELECT moz_bookmarks.title, moz_places.url FROM moz_bookmarks JOIN moz_places ON moz_bookmarks.fk = moz_places.id WHERE moz_bookmarks.type = 1";
				SqliteCommand selectCommand = new(selectSql, connection);
				using SqliteDataReader reader = selectCommand.ExecuteReader();
				while (reader.Read())
				{
					string title = reader.GetString(0);
					string url = reader.GetString(1);
					favorites.Add(new FavItem
					{
						Title = title,
						Url = url,
						IconUrlPath = $"https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={url}&size=32"
					});
				}
				break; // We only need to process one profile
			}
		}

		return favorites;
	}

	private List<FavItem> ImportEdgeBookmarks(string bookmarksPath)
	{
		// Edge uses the same format as Chrome
		return ImportChromeBookmarks(bookmarksPath);
	}
}
