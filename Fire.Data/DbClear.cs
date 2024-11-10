using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace FireBrowserDatabase
{
    public class DbClear
    {
        public static async Task ClearTable(string databasePath, string tableName)
        {
            if (string.IsNullOrEmpty(databasePath) || string.IsNullOrEmpty(tableName))
                return;

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {tableName}";
            await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();
        }
    }
}