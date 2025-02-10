using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Graphite.UserSys;

namespace Graphite.Migration
{
    public sealed partial class ImportWindow : Window
    {
        private MigrationManager _migrationManager;

        public ImportWindow()
        {
            this.InitializeComponent();
            LoadUsersAsync();
        }

        private async void LoadUsersAsync()
        {
            try
            {
                UserComboBox.ItemsSource = await _migrationManager.GetAvailableUsersAsync();
                UserComboBox.DisplayMemberPath = "Username";
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = $"Error loading users: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = UserComboBox.SelectedItem as User;
            if (selectedUser == null)
            {
                await ShowErrorDialog("Please select a user.");
                return;
            }

            string selectedBrowser = (BrowserComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(selectedBrowser))
            {
                await ShowErrorDialog("Please select a browser.");
                return;
            }

            ImportButton.IsEnabled = false;
            ImportProgressRing.IsActive = true;
            StatusTextBlock.Text = "Importing data...";

            try
            {
                bool result = await _migrationManager.ImportFromBrowser(selectedUser.Username, selectedBrowser);
                if (result)
                {
                    StatusTextBlock.Text = "Import completed successfully.";
                }
                else
                {
                    StatusTextBlock.Text = "Import failed.";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                ImportButton.IsEnabled = true;
                ImportProgressRing.IsActive = false;
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }
    }
}

