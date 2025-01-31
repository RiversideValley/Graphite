using Graphite.UserSys;
using Graphite.UserSys.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using Windows.Storage;

namespace Graphite
{
    public sealed partial class Welcome : Window
    {
        private User _currentUser;

        public Welcome(User user)
        {
            this.InitializeComponent();
            _currentUser = user;
            LoadUserData();
        }

        private async void LoadUserData()
        {
            WelcomeMessage.Text = $"Welcome, {_currentUser.Username}!";
            UserEmail.Text = _currentUser.Email ?? "No email provided";

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath))
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(_currentUser.ProfileImagePath);
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        UserProfilePicture.ProfilePicture = bitmapImage;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and use a default image
                    System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
                }
            }
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProfile createProfileWindow = new CreateProfile(_currentUser);
            createProfileWindow.Activate();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement browse functionality
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open settings page
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            // Open help page or show help dialog
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement logout functionality
            this.Close();
        }
    }
}