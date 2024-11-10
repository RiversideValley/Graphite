using Fire.Browser.Core;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

public class SaveDb
{
	public static void InsertHistoryItem(User user, string url, string title, int visitCount, int typedCount, int hidden)
	{
		try
		{
			if (user == null) return;

			string username = user.Username; // Replace 'Username' with the actual property that holds the username in your User model.
			string databaseFilePath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Database", "History.db");
			var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

			if (File.Exists(databaseFilePath))
			{
				using var connection = new SqliteConnection($"Data Source={databaseFilePath}");
				connection.Open();

				using var transaction = connection.BeginTransaction();

				string query = "INSERT INTO urls (url, title, visit_count, typed_count, hidden, last_visit_time) " +
							   "VALUES (@url, @title, @visitCount, @typedCount, @hidden, @last_visit_time)";

				using var command = new SqliteCommand(query, connection, transaction);

				command.Parameters.AddWithValue("@url", url);
				command.Parameters.AddWithValue("@title", title);
				command.Parameters.AddWithValue("@visitCount", visitCount);
				command.Parameters.AddWithValue("@typedCount", typedCount);
				command.Parameters.AddWithValue("@hidden", hidden);
				command.Parameters.AddWithValue("@last_visit_time", currentTime);

				command.ExecuteNonQuery();

				transaction.Commit();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error inserting history item: {ex.Message}");
		}
	}
}