using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Graphite.Setup.OOBE;

public sealed partial class OOBEUser : Page
{
    private bool _isPasswordVisible = false;
    private List<UserImageItem> userImages;
    public OOBEUser()
    {
        this.InitializeComponent();

        LoadUserImages();
    }
    private void UserImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserImageComboBox.SelectedItem is UserImageItem selectedImage)
        {
            UserImagePreview.ProfilePicture = new BitmapImage(new Uri(selectedImage.ImagePath));
            ValidateFields(); // Revalidate fields when image selection changes
        }
    }

    private async void LoadUserImages()
    {
        userImages = new List<UserImageItem>();
        var assetsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Graphite.UserSys", "Assets"));
        var imageFiles = await assetsFolder.GetFilesAsync();

        foreach (var file in imageFiles.Where(f => f.FileType == ".png" || f.FileType == ".jpg"))
        {
            userImages.Add(new UserImageItem
            {
                Name = Path.GetFileNameWithoutExtension(file.Name),
                ImagePath = $"ms-appx:///Graphite.UserSys/Assets/{file.Name}"
            });
        }

        UserImageComboBox.ItemsSource = userImages;
        if (userImages.Any())
        {
            UserImageComboBox.SelectedIndex = 0;
        }
    }

    public class UserImageItem
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
    }

    private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LiveUsernameUpdate.Text = UsernameTextBox.Text;
    }

    private void CreateUserButton_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = PasswordBox.Password;
        string imagePath = (UserImageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        string emailAddress = EmailTextBox.Text;

     
            DisableInputs();
            NextStepButton.Visibility = Visibility.Visible;
              
    }

    private void DisableInputs()
    {
        UserImageComboBox.IsEnabled = false;
        UsernameTextBox.IsEnabled = false;
        PasswordBox.IsEnabled = false;
        EmailTextBox.IsEnabled = false;
        CreateUserButton.IsEnabled = false;
    }

    private void ValidateFields()
    {
        // First, check if an image is selected
        bool imageSelected = UserImageComboBox.SelectedItem != null;


    }
    private async void NextStepButton_Click(object sender, RoutedEventArgs e)
    {
        // Revalidate fields before proceeding
        ValidateFields();

        if (!NextStepButton.IsEnabled)
        {
            return; // Don't proceed if validation fails
        }

        try
        {
            var selectedImage = UserImageComboBox.SelectedItem as UserImageItem;
            if (selectedImage == null)
            {
                return;
            }

            // Convert the ms-appx URI to a StorageFile
            var imageUri = new Uri(selectedImage.ImagePath);
            var imageFile = await StorageFile.GetFileFromApplicationUriAsync(imageUri);

            // Open the file as a stream
            using (var imageStream = await imageFile.OpenReadAsync())
            {
                // Create a MemoryStream from the file stream
                var memoryStream = new MemoryStream();
                await imageStream.AsStreamForRead().CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset the position to the beginning of the stream

                // Call CreateUserAsync with the MemoryStream
                await UserManager.CreateUserAsync(
                    UsernameTextBox.Text,
                    PasswordBox.Password,
                    EmailTextBox.Text,
                    memoryStream
                );
            }

            var authenticatedUser = await UserManager.AuthenticateAsync(UsernameTextBox.Text, PasswordBox.Password);

            if (authenticatedUser != null)
            {
                // Navigate to the next page
                Frame.Navigate(typeof(OOBEPreferences), authenticatedUser);
            }
            else
            {
                // Show an error message if authentication failed
                var dialog = new ContentDialog
                {
                    Title = "Authentication Failed",
                    Content = "Invalid username or password. Please try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog helpDialog = new ContentDialog
        {
            Title = "Account Creation Help",
            Content = "To create an account:\n\n1. Choose a profile picture\n2. Enter a username\n3. Enter a strong password\n4. Click 'Create Account'\n\nIf you need further assistance, please contact support.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        _ = helpDialog.ShowAsync();
    }

    private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        if (_isPasswordVisible)
        {
            PasswordBox.PasswordRevealMode = PasswordRevealMode.Visible;
            PasswordVisibilityIcon.Glyph = "\uE7B2"; // Eye with slash icon
        }
        else
        {
            PasswordBox.PasswordRevealMode = PasswordRevealMode.Hidden;
            PasswordVisibilityIcon.Glyph = "\uE7B3"; // Eye icon
        }
    }

    private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LiveEmailUpdate.Text = EmailTextBox.Text;
    }
}