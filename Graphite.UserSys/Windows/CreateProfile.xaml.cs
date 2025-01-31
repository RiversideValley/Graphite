using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Graphite.UserSys.Windows
{
    public sealed partial class CreateProfile : Window
    {
        private User _currentUser;

        public CreateProfile(User user)
        {
            this.InitializeComponent();
            _currentUser = user;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string profileName = ProfileNameTextBox.Text.Trim();
            string profileType = (ProfileTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string profileDescription = ProfileDescriptionTextBox.Text.Trim();

            if (string.IsNullOrEmpty(profileName))
            {
                await ShowErrorMessageAsync("Profile name cannot be empty.");
                return;
            }

            if (string.IsNullOrEmpty(profileType))
            {
                await ShowErrorMessageAsync("Please select a profile type.");
                return;
            }

            // TODO: Implement profile creation logic here
            // For example:
            await UserManager.AddProfileAsync(_currentUser.Username.ToString(), profileName);

            this.Close();
        }

        private async System.Threading.Tasks.Task ShowErrorMessageAsync(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK"
            };

            await errorDialog.ShowAsync();
        }
    }
}