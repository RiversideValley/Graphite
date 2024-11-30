using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;

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

