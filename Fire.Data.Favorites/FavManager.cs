using FireBrowserWinUi3MultiCore;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FireBrowserWinUi3Favorites;
public class FavManager
{
    private readonly string _favoritesDbPath;

    public FavManager()
    {
        _favoritesDbPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "Favorites.db");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        // Create the directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(_favoritesDbPath));

        // Create or open the SQLite database file
        using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
        {
            connection.Open();

            // Create the Favorites table if it doesn't exist
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS Favorites (
                    Title TEXT,
                    Url TEXT,
                    IconUrlPath TEXT
                )";
            var createTableCommand = new SqliteCommand(createTableSql, connection);
            createTableCommand.ExecuteNonQuery();
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
            using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
            {
                connection.Open();
                foreach (var favItem in favoritesList)
                {
                    var insertSql = "INSERT INTO Favorites (Title, Url, IconUrlPath) VALUES (@Title, @Url, @IconUrlPath)";
                    var insertCommand = new SqliteCommand(insertSql, connection);
                    insertCommand.Parameters.AddWithValue("@Title", favItem.Title);
                    insertCommand.Parameters.AddWithValue("@Url", favItem.Url);
                    insertCommand.Parameters.AddWithValue("@IconUrlPath", favItem.IconUrlPath);
                    insertCommand.ExecuteNonQuery();
                }
            }

            // Remove the old favorites.json file
            File.Delete(oldFavoritesJsonPath);
        }
    }

    public void SaveFav(string title, string url)
    {
        using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
        {
            connection.Open();
            var insertSql = "INSERT INTO Favorites (Title, Url, IconUrlPath) VALUES (@Title, @Url, @IconUrlPath)";
            var insertCommand = new SqliteCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@Title", title);
            insertCommand.Parameters.AddWithValue("@Url", url);
            insertCommand.Parameters.AddWithValue("@IconUrlPath", $"https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={url}&size=32");
            insertCommand.ExecuteNonQuery();
        }
    }

    public List<FavItem> LoadFav()
    {
        var favorites = new List<FavItem>();
        using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
        {
            connection.Open();
            var selectSql = "SELECT * FROM Favorites";
            var selectCommand = new SqliteCommand(selectSql, connection);
            using (var reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    var favItem = new FavItem
                    {
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        Url = reader.GetString(reader.GetOrdinal("Url")),
                        IconUrlPath = reader.GetString(reader.GetOrdinal("IconUrlPath"))
                    };
                    favorites.Add(favItem);
                }
            }
        }
        return favorites;
    }

    public void ClearFavs()
    {
        using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
        {
            connection.Open();
            var deleteSql = "DELETE FROM Favorites";
            var deleteCommand = new SqliteCommand(deleteSql, connection);
            deleteCommand.ExecuteNonQuery();
        }
    }

    public void RemoveFavorite(FavItem selectedItem)
    {
        if (selectedItem == null)
        {
            // Handle the case where the selected item is null (optional)
            Console.WriteLine("Selected item is null.");
            return;
        }

        using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
        {
            connection.Open();
            var deleteSql = "DELETE FROM Favorites WHERE Title = @Title AND Url = @Url";
            var deleteCommand = new SqliteCommand(deleteSql, connection);
            deleteCommand.Parameters.AddWithValue("@Title", selectedItem.Title);
            deleteCommand.Parameters.AddWithValue("@Url", selectedItem.Url);
            deleteCommand.ExecuteNonQuery();
        }
    }

    public void ImportFavoritesFromOtherBrowsers(string browserName)
    {
        string favoritesPath = "";
        List<FavItem> importedFavorites = new List<FavItem>();

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
        using (var connection = new SqliteConnection($"Data Source={_favoritesDbPath}"))
        {
            connection.Open();
            foreach (var favItem in importedFavorites)
            {
                var insertSql = "INSERT INTO Favorites (Title, Url, IconUrlPath) VALUES (@Title, @Url, @IconUrlPath)";
                var insertCommand = new SqliteCommand(insertSql, connection);
                insertCommand.Parameters.AddWithValue("@Title", favItem.Title);
                insertCommand.Parameters.AddWithValue("@Url", favItem.Url);
                insertCommand.Parameters.AddWithValue("@IconUrlPath", favItem.IconUrlPath);
                insertCommand.ExecuteNonQuery();
            }
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
        List<FavItem> favorites = new List<FavItem>();

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
        List<FavItem> favorites = new List<FavItem>();

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
                using (var connection = new SqliteConnection($"Data Source={placesPath}"))
                {
                    connection.Open();
                    var selectSql = "SELECT moz_bookmarks.title, moz_places.url FROM moz_bookmarks JOIN moz_places ON moz_bookmarks.fk = moz_places.id WHERE moz_bookmarks.type = 1";
                    var selectCommand = new SqliteCommand(selectSql, connection);
                    using (var reader = selectCommand.ExecuteReader())
                    {
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
                    }
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
