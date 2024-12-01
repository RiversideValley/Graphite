using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Microsoft.Data.Sqlite;
using System.Reflection;
using Riverside.Graphite.Core;

namespace Riverside.Graphite
{
	public sealed partial class UpdateChecker : Window, INotifyPropertyChanged
	{
		private string _oldVersion;
		private string _newVersion;

		public string OldVersion
		{
			get => _oldVersion;
			private set
			{
				if (_oldVersion != value)
				{
					_oldVersion = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OldVersion)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigVersionInfo)));
				}
			}
		}

		public string NewVersion
		{
			get => _newVersion;
			private set
			{
				if (_newVersion != value)
				{
					_newVersion = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewVersion)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstalledVersionInfo)));
				}
			}
		}

		public string ConfigVersionInfo => $"Config File Version: {OldVersion}";
		public string InstalledVersionInfo => $"Installed App Version: {NewVersion}";

		public event PropertyChangedEventHandler PropertyChanged;

		public UpdateChecker()
		{
			this.InitializeComponent();
			this.Activated += UpdateChecker_Activated;
		}

		private async void UpdateChecker_Activated(object sender, WindowActivatedEventArgs args)
		{
			// Only run this once when the window is first activated
			this.Activated -= UpdateChecker_Activated;

			await LoadVersions();
			await PatchApplication();
		}

		private async Task LoadVersions()
		{
			try
			{
				// Load old version from config.json
				string fireBrowserUserCorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
				string configFilePath = Path.Combine(fireBrowserUserCorePath, "config.json");

				if (File.Exists(configFilePath))
				{
					string jsonContent = await File.ReadAllTextAsync(configFilePath);
					var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
					OldVersion = config["app.version"].ToString();
				}
				else
				{
					OldVersion = "Unknown";
				}

				// Load new version from package
				PackageVersion currentVersion = Package.Current.Id.Version;
				NewVersion = $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}.{currentVersion.Revision}";
			}
			catch (Exception ex)
			{
				StatusMessage.Text = $"Error loading versions: {ex.Message}";
			}
		}

		private async Task PatchApplication()
		{
			try
			{
				await Task.Delay(1000); // Simulating some initialization
				StatusMessage.Text = "Analyzing version differences...";
				await SimulateProgressBar(20);

				await Task.Delay(1000);
				StatusMessage.Text = "Preparing patch...";
				await SimulateProgressBar(40);

				await Task.Delay(1000);
				StatusMessage.Text = "Applying patch...";
				await SimulateProgressBar(50);

				// Execute SQL update
				await ExecuteSqlUpdate();
				await SimulateProgressBar(70);

				await Task.Delay(1000);
				StatusMessage.Text = "Verifying patch...";
				await SimulateProgressBar(90);

				await Task.Delay(1000);
				StatusMessage.Text = "Updating configuration...";
				await UpdateConfigFile();

				StatusMessage.Text = "Patching completed successfully!";
				UpdateProgressBar.IsIndeterminate = false;
				UpdateProgressBar.Value = 100;

				CloseButton.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				StatusMessage.Text = $"Error during patching: {ex.Message}";
			}
		}

		private async Task ExecuteSqlUpdate()
		{
			try
			{
				// Get the SQL update file content
				string sqlUpdateContent = await GetSqlUpdateContent();

				// Execute SQL update on all user databases
				string fireBrowserUserCorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
				string[] userFolders = Directory.GetDirectories(Path.Combine(fireBrowserUserCorePath, "Users", AuthService.CurrentUser.Username, "Settings"));

				foreach (string userFolder in userFolders)
				{
					string dbPath = Path.Combine(userFolder, "settings.db");
					await ExecuteSqlCommandsOnDatabase(dbPath, sqlUpdateContent);
				}

				// Execute SQL update on settings.db
				string settingsDbPath = Path.Combine(fireBrowserUserCorePath, "settings.db");
				await ExecuteSqlCommandsOnDatabase(settingsDbPath, sqlUpdateContent);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to execute SQL update: {ex.Message}");
			}
		}

		private async Task<string> GetSqlUpdateContent()
		{
			try
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = "Riverside.Graphite.Data.Core.update.sql";

				using (Stream stream = assembly.GetManifestResourceStream(resourceName))
				using (StreamReader reader = new StreamReader(stream))
				{
					return await reader.ReadToEndAsync();
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to read SQL update file: {ex.Message}");
			}
		}

		private async Task ExecuteSqlCommandsOnDatabase(string dbPath, string sqlCommands)
		{
			if (!File.Exists(dbPath))
			{
				return; // Skip if database doesn't exist
			}

			using (var connection = new SqliteConnection($"Data Source={dbPath}"))
			{
				await connection.OpenAsync();

				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						using (var command = connection.CreateCommand())
						{
							command.CommandText = sqlCommands;
							await command.ExecuteNonQueryAsync();
						}

						transaction.Commit();
					}
					catch
					{
						transaction.Rollback();
						throw;
					}
				}
			}
		}

		private async Task UpdateConfigFile()
		{
			try
			{
				string fireBrowserUserCorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
				string configFilePath = Path.Combine(fireBrowserUserCorePath, "config.json");

				var config = new Dictionary<string, object>();
				if (File.Exists(configFilePath))
				{
					string jsonContent = await File.ReadAllTextAsync(configFilePath);
					config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
				}

				config["app.version"] = NewVersion;

				string updatedJsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
				await File.WriteAllTextAsync(configFilePath, updatedJsonContent);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to update config file: {ex.Message}");
			}
		}

		private async Task SimulateProgressBar(int targetProgress)
		{
			UpdateProgressBar.IsIndeterminate = false;
			while (UpdateProgressBar.Value < targetProgress)
			{
				UpdateProgressBar.Value += 1;
				await Task.Delay(50);
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}

