using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.UserSys;

public class ProfileManager
{
    private readonly string _username;
    private readonly string _dbPath;

    public ProfileManager(string username)
    {
        _username = username;
        _dbPath = Path.Combine(UserManager.GraphiteDataPath, username, "Database", "userprofiles.db");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ProfileNames (
                    Name TEXT PRIMARY KEY,
                    Type TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );";
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<(string Name, string Type, bool IsActive)>> GetProfilesAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Type, IsActive FROM ProfileNames";

        var profiles = new List<(string Name, string Type, bool IsActive)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string name = reader.GetString(0);
            string type = reader.GetString(1);
            bool isActive = reader.GetInt32(2) != 0;
            profiles.Add((name, type, isActive));
        }

        return profiles;
    }

    public async Task<bool> AddProfileAsync(string profileName, string profileType)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO ProfileNames (Name, Type, IsActive) VALUES ($name, $type, 1)";
            command.Parameters.AddWithValue("$name", profileName);
            command.Parameters.AddWithValue("$type", profileType);

            int rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                command.CommandText = $@"
                        CREATE TABLE IF NOT EXISTS profiledata_{profileName} (
                            Key TEXT PRIMARY KEY,
                            Value TEXT NOT NULL
                        )";
                await command.ExecuteNonQueryAsync();

                await InitializeProfileDataAsync(connection, profileName);
            }

            await transaction.CommitAsync();
            return rowsAffected > 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteProfileAsync(string profileName)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var command = connection.CreateCommand();
            command.CommandText = $"DROP TABLE IF EXISTS profiledata_{profileName}";
            await command.ExecuteNonQueryAsync();

            command.CommandText = "DELETE FROM ProfileNames WHERE Name = $name";
            command.Parameters.AddWithValue("$name", profileName);
            int rowsAffected = await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return rowsAffected > 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateProfileStatusAsync(string profileName, bool isActive)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE ProfileNames SET IsActive = $isActive WHERE Name = $name";
        command.Parameters.AddWithValue("$isActive", isActive ? 1 : 0);
        command.Parameters.AddWithValue("$name", profileName);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<Dictionary<string, string>> GetProfileDataAsync(string profileName)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $"SELECT Key, Value FROM profiledata_{profileName}";

        var data = new Dictionary<string, string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string key = reader.GetString(0);
            string value = reader.GetString(1);
            data[key] = value;
        }

        return data;
    }

    public async Task<bool> UpdateProfileDataAsync(string profileName, string key, string value)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $@"
                INSERT OR REPLACE INTO profiledata_{profileName} (Key, Value)
                VALUES ($key, $value)";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateTabsAsync(string profileName, List<string> tabUrls)
    {
        string tabsJson = System.Text.Json.JsonSerializer.Serialize(tabUrls);
        return await UpdateProfileDataAsync(profileName, "OpenTabs", tabsJson);
    }

    public async Task<List<string>> GetTabsAsync(string profileName)
    {
        var profileData = await GetProfileDataAsync(profileName);
        if (profileData.TryGetValue("OpenTabs", out string tabsJson))
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(tabsJson);
        }
        return new List<string>();
    }

    private async Task InitializeProfileDataAsync(SqliteConnection connection, string profileName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $@"
                INSERT OR IGNORE INTO profiledata_{profileName} (Key, Value)
                VALUES 
                ('OpenTabs', '[]'),
                ('LastActive', '{DateTime.UtcNow:O}')";
        await command.ExecuteNonQueryAsync();
    }
}
