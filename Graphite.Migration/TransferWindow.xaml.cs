using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Graphite.Migration
{
    public sealed partial class TransferWindow : Window
    {
        public TransferWindow()
        {
            this.InitializeComponent();
            LoadUsersAsync();
        }

        private async void LoadUsersAsync()
        {
            try
            {
                // Simulate loading users
                await Task.Delay(1000);
                var users = new List<string> { "User1", "User2", "User3" };
                UserComboBox.ItemsSource = users;
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error loading users: {ex.Message}");
            }
        }

        private async void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = UserComboBox.SelectedItem as string;
            if (selectedUser == null)
            {
                await ShowErrorDialog("Please select a user.");
                return;
            }

            string password = PasswordBox.Password;
            string destinationIp = DestinationIpTextBox.Text;

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(destinationIp))
            {
                await ShowErrorDialog("Please fill in all fields.");
                return;
            }

            TransferButton.IsEnabled = false;
            TransferProgressRing.IsActive = true;
            StatusTextBlock.Text = "Simulating data transfer...";

            await SimulateDataTransfer();

            TransferButton.IsEnabled = true;
            TransferProgressRing.IsActive = false;
        }

        private async Task SimulateDataTransfer()
        {
            await Task.Delay(3000); // Simulate 3-second transfer
            StatusTextBlock.Text = "Transfer completed successfully.";
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}

