using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Graphite.UserSys.Windows
{
	public sealed partial class BrowserImportScreen : Window
	{
		public BrowserImportScreen()
		{
			this.InitializeComponent();
		}

		private async void ImportButton_Click(object sender, RoutedEventArgs e)
		{
			string selectedBrowser = (BrowserSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
			string username = UsernameInput.Text;

			if (string.IsNullOrWhiteSpace(selectedBrowser) || string.IsNullOrWhiteSpace(username))
			{
				ShowStatus("Please select a browser and enter a username.");
				return;
			}

			ImportButton.IsEnabled = false;
			ImportProgress.Visibility = Visibility.Visible;
			StatusMessage.Visibility = Visibility.Visible;

			try
			{
				var importer = new BrowserImport(username);
				await ImportDataAsync(importer, selectedBrowser);
				ShowStatus("Import completed successfully!");
			}
			catch (Exception ex)
			{
				ShowStatus($"Error during import: {ex.Message}");
			}
			finally
			{
				ImportButton.IsEnabled = true;
				ImportProgress.Visibility = Visibility.Collapsed;
			}
		}

		private async Task ImportDataAsync(BrowserImport importer, string browser)
		{
			Func<Task> importTask = browser switch
			{
				"Microsoft Edge" => importer.ImportFromEdgeAsync,
				"Google Chrome" => importer.ImportFromChromeAsync,
				"Mozilla Firefox" => importer.ImportFromFirefoxAsync,
				"Arc" => importer.ImportFromArcAsync,
				"Brave" => importer.ImportFromBraveAsync,
				_ => throw new ArgumentException("Unsupported browser")
			};

			await importTask();
		}

		private void ShowStatus(string message)
		{
			StatusMessage.Text = message;
			StatusMessage.Visibility = Visibility.Visible;
		}
	}
}
