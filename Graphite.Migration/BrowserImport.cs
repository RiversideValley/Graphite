using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;

namespace Graphite.Migration
{
    public class BrowserImport
    {
        private readonly string _graphiteDataPath;

        public BrowserImport(string graphiteDataPath)
        {
            _graphiteDataPath = graphiteDataPath;
        }

        public async Task<bool> ImportFromBrowserAsync(string browserName, string username)
        {
            string browserDataPath = GetBrowserDataPath(browserName);

            await ImportHistoryAsync(browserDataPath, browserName, username);
            await ImportDownloadsAsync(browserDataPath, browserName, username);
            await ImportFavoritesAsync(browserDataPath, browserName, username);

            return true;
        }

        private string GetBrowserDataPath(string browserName)
        {
            return browserName.ToLower() switch
            {
                "edge" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data\Default"),
                "chrome" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default"),
                "firefox" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles"),
                "brave" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BraveSoftware\Brave-Browser\User Data\Default"),
                _ => throw new ArgumentException("Unsupported browser", nameof(browserName))
            };
        }

        private async Task ImportHistoryAsync(string browserDataPath, string browserName, string username)
        {
            string historyDbPath = Path.Combine(browserDataPath, "History");
            string graphiteHistoryDbPath = Path.Combine(_graphiteDataPath, username, "History.db");

            await ImportDatabaseAsync(historyDbPath, graphiteHistoryDbPath, "urls", "History", 
                new[] { "url", "title", "last_visit_time" }, 
                new[] { "Url", "Title", "VisitTime" }, browserName);
        }

        private async Task ImportDownloadsAsync(string browserDataPath, string browserName, string username)
        {
            string downloadsDbPath = Path.Combine(browserDataPath, "History");
            string graphiteDownloadsDbPath = Path.Combine(_graphiteDataPath, username, "Downloads.db");

            await ImportDatabaseAsync(downloadsDbPath, graphiteDownloadsDbPath, "downloads", "Downloads", 
                new[] { "target_path", "tab_url", "start_time", "received_bytes", "total_bytes" }, 
                new[] { "FilePath", "SourceUrl", "StartTime", "ReceivedBytes", "TotalBytes" }, browserName);
        }

        private async Task ImportFavoritesAsync(string browserDataPath, string browserName, string username)
        {
            if (browserName.ToLower() == "firefox")
            {
                await ImportFirefoxFavoritesAsync(browserDataPath, username);
            }
            else
            {
                await ImportChromiumFavoritesAsync(browserDataPath, browserName, username);
            }
        }

        private async Task ImportDatabaseAsync(string sourceDbPath, string destDbPath, string sourceTable, string destTable, string[] sourceColumns, string[] destColumns, string browserName)
        {
            if (!File.Exists(sourceDbPath))
            {
                Console.WriteLine($"{browserName} {sourceTable} database not found.");
                return;
            }

            try
            {
                string tempDbPath = Path.GetTempFileName();
                File.Copy(sourceDbPath, tempDbPath, true);

                using var sourceConnection = new SqliteConnection($"Data Source={tempDbPath};Mode=ReadOnly");
                await sourceConnection.OpenAsync();

                Directory.CreateDirectory(Path.GetDirectoryName(destDbPath));
                using var destConnection = new SqliteConnection($"Data Source={destDbPath}");
                await destConnection.OpenAsync();

                var command = sourceConnection.CreateCommand();
                command.CommandText = $"SELECT {string.Join(", ", sourceColumns)} FROM {sourceTable} ORDER BY last_visit_time DESC LIMIT 1000";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var insertCommand = destConnection.CreateCommand();
                    insertCommand.CommandText = $"INSERT OR IGNORE INTO {destTable} ({string.Join(", ", destColumns)}, Source) VALUES ({string.Join(", ", destColumns.Select(c => "$" + c))}, $Source)";

                    for (int i = 0; i < sourceColumns.Length; i++)
                    {
                        insertCommand.Parameters.AddWithValue("$" + destColumns[i], reader.GetValue(i));
                    }
                    insertCommand.Parameters.AddWithValue("$Source", browserName);

                    await insertCommand.ExecuteNonQueryAsync();
                }

                File.Delete(tempDbPath);
                Console.WriteLine($"Imported {sourceTable} from {browserName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing {browserName} {sourceTable}: {ex.Message}");
            }
        }

        private async Task ImportFirefoxFavoritesAsync(string browserDataPath, string username)
        {
            // Implementation for Firefox favorites import
            // You'll need to adjust this method to work with the new file structure
        }

        private async Task ImportChromiumFavoritesAsync(string browserDataPath, string browserName, string username)
        {
            // Implementation for Chromium-based browsers favorites import
            // You'll need to adjust this method to work with the new file structure
        }
    }
}

