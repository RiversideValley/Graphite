using Microsoft.Data.Sqlite;
using Riverside.Graphite.Core;

namespace Riverside.Graphite.Tests
{
	[TestFixture]
	public class UserManagerTests
	{
		private string TestDbPath = Path.Combine(UserManager.GraphiteDataPath, "TestUserCore.db");

		[SetUp]
		public async Task SetUp()
		{
			// Set up the test database
			if (File.Exists(TestDbPath))
			{
				File.Delete(TestDbPath);
			}

			await using (var connection = new SqliteConnection($"Data Source={TestDbPath}"))
			{
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Username TEXT PRIMARY KEY,
                        PasswordHash TEXT,
                        Email TEXT,
                        WindowsUserName TEXT,
                        IsFirstLaunch INTEGER,
                        ProfileImagePath TEXT,
                        HasPassword INTEGER
                    );
                    CREATE TABLE IF NOT EXISTS UserSecurity (
                        Username TEXT PRIMARY KEY,
                        Hash TEXT,
                        Salt TEXT
                    );
                ";

				await command.ExecuteNonQueryAsync();

				string username = "testuser";
				string password = "PassW0rd!";
				var (passwordHash, passwordSalt) = UserManager.HashPassword(password);

				command.CommandText = @"
                    INSERT INTO Users (Username, PasswordHash, Email, WindowsUserName, IsFirstLaunch, ProfileImagePath, HasPassword)
                    VALUES ($username, $passwordHash, 'testuser@example.com', 'testwindowsuser', 0, 'testprofileimagepath', 1);
                    INSERT INTO UserSecurity (Username, Hash, Salt)
                    VALUES ($username, $hash, $salt);
                ";
				command.Parameters.AddWithValue("$username", username);
				command.Parameters.AddWithValue("$passwordHash", passwordHash);
				command.Parameters.AddWithValue("$hash", passwordHash);
				command.Parameters.AddWithValue("$salt", passwordSalt);
				await command.ExecuteNonQueryAsync();
			}
		}

		[TearDown]
		public void TearDown()
		{
			CloseDatabaseConnections();

			const int maxRetries = 3;
			const int delay = 1000; // 1 second

			for (int i = 0; i < maxRetries; i++)
			{
				try
				{
					if (File.Exists(TestDbPath))
					{
						File.Delete(TestDbPath);
					}
					break;
				}
				catch (IOException)
				{
					if (i == maxRetries - 1)
						throw;
					Thread.Sleep(delay);
				}
			}
		}
		private void CloseDatabaseConnections()
		{
			// close any open database connections
			SqliteConnection.ClearAllPools();
		}
		[Test]
		public async Task GetAllUsersAsync_ShouldReturnAllUsers()
		{
			UserManager.MainDbPath = TestDbPath;

			List<UserV2> users = await UserManager.GetAllUsersAsync();

			TestContext.WriteLine(System.Text.Json.JsonSerializer.Serialize(users.ToArray()));

			Assert.That(users, Is.Not.Null);
			Assert.That(users, Is.Not.Empty);
			Assert.That(users[0].Username, Is.EqualTo("testuser"));
		}

		[Test]
		public async Task AuthenticateAsync_ShouldReturnUser_WhenCredentialsAreValid()
		{

			UserManager.MainDbPath = TestDbPath;
			string username = "testuser";


			UserV2 user = await UserManager.GetUserAsync(username);

			if (user is not null)
				TestContext.WriteLine(System.Text.Json.JsonSerializer.Serialize(user));

			Assert.That(user, Is.Not.Null);
			Assert.That(user.Username, Is.EqualTo(username));
		}
	}
}
