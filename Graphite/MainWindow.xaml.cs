using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Graphite.UserSys;

namespace Graphite
{
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<UserViewModel> Users { get; set; }

        public MainWindow()
        {
            this.InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await UserManager.InitializeAsync();
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                Users = new ObservableCollection<UserViewModel>();
                string graphiteDataPath = UserManager.GraphiteDataPath;

                // Create the directory if it doesn't exist
                Directory.CreateDirectory(graphiteDataPath);

                // Get all user directories
                var userDirs = Directory.GetDirectories(graphiteDataPath);
                foreach (var userDir in userDirs)
                {
                    var username = Path.GetFileName(userDir);
                    if (!username.Equals("Guest", StringComparison.OrdinalIgnoreCase))
                    {
                        var profileImagePath = Path.Combine(userDir, "profile_image.jpg");

                        var userViewModel = new UserViewModel
                        {
                            Username = username,
                            ProfileImageSource = await LoadProfileImageAsync(profileImagePath)
                        };

                        Users.Add(userViewModel);
                    }
                }

                UserListView.ItemsSource = Users;
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Failed to load users: {ex.Message}");
            }
        }

        private async Task<BitmapImage> LoadProfileImageAsync(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                if (File.Exists(imagePath))
                {
                    using (var stream = File.OpenRead(imagePath))
                    {
                        using (var randomAccessStream = new InMemoryRandomAccessStream())
                        {
                            await RandomAccessStream.CopyAsync(stream.AsInputStream(), randomAccessStream);
                            randomAccessStream.Seek(0);
                            await bitmap.SetSourceAsync(randomAccessStream);
                        }
                    }
                }
                return bitmap;
            }
            catch
            {
                return new BitmapImage();
            }
        }

        private void UserListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is UserViewModel selectedUser)
            {
                AttemptLoginAsync(selectedUser.Username);
            }
        }

        private async Task AttemptLoginAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    await ShowErrorMessageAsync("Please enter a username.");
                    return;
                }

                var user = await UserManager.GetUserAsync(username);
                if (user == null)
                {
                    await ShowErrorMessageAsync("User not found.");
                    return;
                }

                if (user.HasPassword)
                {
                    await ShowPasswordDialogAsync(user);
                }
                else
                {
                    var authenticatedUser = await UserManager.AuthenticateAsync(username, null);
                    if (authenticatedUser != null)
                    {
                        // Open the Welcome window
                        Welcome welcomeWindow = new Welcome(authenticatedUser);
                        welcomeWindow.Activate();
                        this.Close(); // Close the login window
                    }
                    else
                    {
                        await ShowErrorMessageAsync("Login failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Login failed: {ex.Message}");
            }
        }

        private async Task ShowPasswordDialogAsync(User user)
        {
            var passwordBox = new PasswordBox { PlaceholderText = "Enter password" };
            var dialog = new ContentDialog
            {
                Title = $"Enter password for {user.Username}",
                PrimaryButtonText = "Login",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = passwordBox,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var authenticatedUser = await UserManager.AuthenticateAsync(user.Username, passwordBox.Password);
                if (authenticatedUser != null)
                {
                    // Open the Welcome window
                    Welcome welcomeWindow = new Welcome(authenticatedUser);
                    welcomeWindow.Activate();
                    this.Close(); // Close the login window
                }
                else
                {
                    await ShowErrorMessageAsync("Invalid password.");
                }
            }
        }

        private async Task ShowErrorMessageAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessMessageAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void CreateNewUser_Click(object sender, RoutedEventArgs e)
        {
            Graphite.Setup.OOBE.SetupWelcome setupWelcome = new Setup.OOBE.SetupWelcome();
            setupWelcome.Activate();
            this.Close();

        }

        private async void OpenGuestUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var guestUser = await UserManager.LoginAsGuestAsync();
                // Navigate to the main application window or page for the guest user
                Welcome welcome = new Welcome(guestUser);
                welcome.Activate();
                this.Close();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Failed to login as Guest: {ex.Message}");
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            ContentDialog messageDialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await messageDialog.ShowAsync();
        }
    }

    public class UserViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public BitmapImage ProfileImageSource { get; set; }
    }
}