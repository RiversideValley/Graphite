using FireBrowserWinUi3Assets;
using FireBrowserWinUi3MultiCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace FireBrowserWinUi3
{
    public sealed partial class SetupUser : Page
    {
        private string selectedImageName = "clippy.png";

        public SetupUser() => InitializeComponent();

        private void ProfileImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileImage.SelectedItem is string selectedItem)
            {
                selectedImageName = $"{selectedItem}.png";
                Pimg.ProfilePicture = new ImageLoader().LoadImage(selectedImageName);
            }
        }

        private void UserName_TextChanged(object sender, TextChangedEventArgs e) =>
            UsrBox.Text = UserName.Text;

        private async void Create_Click(object sender, RoutedEventArgs e) =>
            await CreateUserAndNavigate();

        private async Task CreateUserAndNavigate()
        {
            await CreateUserOnStartup();
            Frame.Navigate(typeof(SetupUi));
        }

        private async Task CreateUserOnStartup()
        {
            FireBrowserWinUi3MultiCore.User newUser = new FireBrowserWinUi3MultiCore.User
            {
                Username = UserName.Text,
            };

            List<FireBrowserWinUi3MultiCore.User> users = new List<FireBrowserWinUi3MultiCore.User> { newUser }; UserFolderManager.CreateUserFolders(newUser);
            UserDataManager.SaveUsers(users);
            AuthService.AddUser(newUser);
            AuthService.Authenticate(newUser.Username);

            await CopyImageToUserDirectory();
        }

        private async Task CopyImageToUserDirectory()
        {
            try
            {
                var destinationFolder = await StorageFolder.GetFolderFromPathAsync(
                    Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username));
                var imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///FireBrowserWinUi3Assets/Assets/{selectedImageName}"));
                await imageFile.CopyAsync(destinationFolder, "profile_image.jpg", NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                // Consider using a logging framework instead of Console.WriteLine
                System.Diagnostics.Debug.WriteLine($"Error copying image: {ex.Message}");
            }
        }
    }
}