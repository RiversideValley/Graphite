using CommunityToolkit.Mvvm.Messaging;
using Riverside.Graphite.Core;
using Riverside.Graphite.Core.Helper;
using Riverside.Graphite.Runtime.CoreUi;
using Riverside.Graphite.Services;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.Services.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Controls;
public sealed partial class ProfileCommander : Flyout
{
	private SettingsService SettingsService { get; set; }
	private MainWindowViewModel MainWindowViewModel { get; set; }
	private IMessenger Messenger { get; set; }

	public ProfileCommander(MainWindowViewModel mainWindowViewModel)
	{
		MainWindowViewModel = mainWindowViewModel;
		SettingsService = App.GetService<SettingsService>();
		Messenger = App.GetService<IMessenger>();

		InitializeComponent();
		LoadUserDataAndSettings();
	}

	private void LoadUserDataAndSettings()
	{
		if (!AuthService.IsUserAuthenticated)
		{
			return;
		}

		UsernameDisplay.Text = SettingsService.CurrentUser.Username ?? "DefaultUser";
	}

	private string iImage = "";

	private async void pfpchanged_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (pfpchanged.SelectedItem != null)
		{
			// Assuming 'userImageName' contains the name of the user's image file
			string userImageName = pfpchanged.SelectedItem.ToString() + ".png"; // Replace this with the actual user's image name

			iImage = userImageName;
			// Instantiate ImageLoader
			ImageHelper imgLoader = new();

			// Use the LoadImage method to get the image
			Microsoft.UI.Xaml.Media.Imaging.BitmapImage userProfilePicture = imgLoader.LoadImage(userImageName);

			MainWindowViewModel.ProfileImage = userProfilePicture;

			// Assign the retrieved image to the ProfileImageControl
			RootImage.ProfilePicture = userProfilePicture;

			string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username);

			await CopyImageAsync(iImage.ToString(), destinationFolderPath);

			PersonPicture pip = MainWindowViewModel.MainView?.TabViewContainer.TabStripHeader as PersonPicture;

			if (pip is PersonPicture person)
			{
				person.ProfilePicture = userProfilePicture;
			}
		}
	}

	public async Task CopyImageAsync(string iImage, string destinationFolderPath)
	{
		ImageHelper imgLoader = new();
		imgLoader.ImageName = iImage;
		_ = imgLoader.LoadImage($"{iImage}");

		StorageFolder destinationFolder = await StorageFolder.GetFolderFromPathAsync(destinationFolderPath);

		StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Riverside.Graphite.Core/Assets/{iImage}"));
		_ = await imageFile.CopyAsync(destinationFolder, "profile_image.jpg", NameCollisionOption.ReplaceExisting);

		Console.WriteLine("Image copied successfully!");
	}


	private async void ChangeUsername_Click(object sender, RoutedEventArgs e)
	{

		_ = Messenger.Send(new Message_Settings_Actions(EnumMessageStatus.Settings));

		string olduser = UsernameDisplay.Text;

		_ = AuthService.ChangeUsername(olduser, username_box.Text.ToString());
		string tempFolderPath = Path.GetTempPath();
		string jsonFilePath = Path.Combine(tempFolderPath, "changeusername.json");
		await File.WriteAllTextAsync(jsonFilePath, JsonConvert.SerializeObject(AuthService.UserWhomIsChanging));

		// must restart due to file locking allocation old user file are in use by webview, and dbservices.
		_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");

	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		string site = "google"; // Default site
		QuickSign win = new(site);
		win.Activate();

	}

	private void Button_Click_1(object sender, RoutedEventArgs e)
	{
		string site = "microsoft"; // Default site
		QuickSign win = new(site);
		win.Activate();
	}
}