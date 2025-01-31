using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;

namespace Graphite.UserSys
{
    public sealed partial class CreateUserWindow : Window
    {
        private StorageFile selectedImageFile;
        private bool isProcessing = false;

        public CreateUserWindow()
        {
            this.InitializeComponent();

            InitializeWindow();
        }

        private void InitializeWindow()
        {
            LoadBuiltInImages();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            UsernameBox.TextChanged += RequiredField_Changed;
            EmailBox.TextChanged += EmailBox_TextChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        private void LoadBuiltInImages()
        {
            try
            {
                ProfileImageComboBox.Items.Clear();
                ProfileImageComboBox.Items.Add(new ComboBoxItem { Content = "Select built-in image" });

                var stockImages = AssetImageLoader.GetStockImageNames();
                foreach (var imageName in stockImages)
                {
                    var item = new ComboBoxItem
                    {
                        Content = imageName,
                        Tag = AssetImageLoader.LoadImage(imageName)
                    };
                    ProfileImageComboBox.Items.Add(item);
                }

                ProfileImageComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to load built-in images: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void RequiredField_Changed(object sender, TextChangedEventArgs e)
        {
            ValidateUsername();
            UpdateCreateButtonState();
        }

        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmail();
            UpdateCreateButtonState();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
            UpdateCreateButtonState();
        }

        private bool ValidateUsername()
        {
            string username = UsernameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowStatus("Username is required", InfoBarSeverity.Warning);
                return false;
            }

            if (!Regex.IsMatch(username, "^[a-zA-Z0-9_-]{3,20}$"))
            {
                ShowStatus("Username must be 3-20 characters and contain only letters, numbers, underscores, and hyphens", InfoBarSeverity.Warning);
                return false;
            }

            StatusInfoBar.IsOpen = false;
            return true;
        }

        private bool ValidateEmail()
        {
            string email = EmailBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email is optional

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowStatus("Invalid email format", InfoBarSeverity.Warning);
                return false;
            }

            return true;
        }

        private bool ValidatePassword()
        {
            string password = PasswordBox.Password;
            if (string.IsNullOrWhiteSpace(password))
                return true; // Password is optional

            if (password.Length < 6)
            {
                ShowStatus("Password must be at least 6 characters long", InfoBarSeverity.Warning);
                return false;
            }

            return true;
        }

        private async void ProfileImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ProfileImageComboBox.SelectedIndex > 0)
                {
                    var selectedItem = ProfileImageComboBox.SelectedItem as ComboBoxItem;
                    SelectedImagePreview.Source = selectedItem.Tag as BitmapImage;
                    selectedImageFile = null;
                }
                else
                {
                    SelectedImagePreview.Source = null;
                }

                UpdateCreateButtonState();
            }
            catch (Exception ex)
            {
                ShowStatus($"Error selecting image: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void SelectCustomImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary
                };

                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                // Initialize the picker with the current window handle
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    selectedImageFile = file;
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        SelectedImagePreview.Source = bitmapImage;
                    }
                    ProfileImageComboBox.SelectedIndex = 0;
                    UpdateCreateButtonState();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error selecting custom image: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void UpdateCreateButtonState()
        {
            bool isUsernameValid = !string.IsNullOrWhiteSpace(UsernameBox.Text) && ValidateUsername();
            bool isEmailValid = ValidateEmail();
            bool isPasswordValid = ValidatePassword();
            bool isImageSelected = selectedImageFile != null || ProfileImageComboBox.SelectedIndex > 0;

            ConfirmButton.IsEnabled = !isProcessing && isUsernameValid && isEmailValid && isPasswordValid && isImageSelected;
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing) return;

            try
            {
                isProcessing = true;
                UpdateCreateButtonState();
                ShowStatus("Creating user...", InfoBarSeverity.Informational);

                var userData = await GetUserDataAsync();
                await UserManager.CreateUserAsync(
                    userData.username,
                    userData.password,
                    userData.email,
                    userData.imageStream
                );

                ShowStatus("User created successfully!", InfoBarSeverity.Success);
                ClearFields();

                // Optional: Close the window after successful creation
                // this.Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to create user: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                isProcessing = false;
                UpdateCreateButtonState();
            }
        }

        private async Task<(string username, string password, string email, Stream imageStream)> GetUserDataAsync()
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string email = EmailBox.Text.Trim();

            Stream imageStream = null;
            if (selectedImageFile != null)
            {
                imageStream = await selectedImageFile.OpenStreamForReadAsync();
            }
            else if (ProfileImageComboBox.SelectedIndex > 0)
            {
                var selectedItem = ProfileImageComboBox.SelectedItem as ComboBoxItem;
                string imageName = selectedItem.Content.ToString();
                string imageUri = AssetImageLoader.GetStockImageUri(imageName);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(imageUri));
                imageStream = await file.OpenStreamForReadAsync();
            }

            return (username, password, email, imageStream);
        }

        private void ClearFields()
        {
            UsernameBox.Text = string.Empty;
            PasswordBox.Password = string.Empty;
            EmailBox.Text = string.Empty;
            ProfileImageComboBox.SelectedIndex = 0;
            SelectedImagePreview.Source = null;
            selectedImageFile = null;
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }
    }
}