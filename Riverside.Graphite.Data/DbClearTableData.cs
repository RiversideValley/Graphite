using Microsoft.Data.Sqlite;

namespace FireBrowserDatabase
{
	public class DbClearTableData
	{
		public void DeleteTableData(string databasePath, string tablename, string where)
		{
			if (string.IsNullOrEmpty(databasePath) || string.IsNullOrEmpty(tablename))
				return;

			using var m_dbConnection = new SqliteConnection($"Data Source={databasePath};");
			m_dbConnection.Open();

			string wheret = string.IsNullOrEmpty(where) ? string.Empty : $" where {where}";
			using var command = new SqliteCommand($"delete from {tablename}{wheret}", m_dbConnection);
			command.ExecuteNonQuery();

			m_dbConnection.Close();
		}
	}
}