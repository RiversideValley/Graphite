using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Pages.Patch;
using System;
using System.Threading.Tasks;

namespace Riverside.Graphite;

public sealed partial class SetupInit : Page
{
	UC_Viewmodel Uc_Viewmodel;
	private const string IntroMessage = @"
� Seamless browsing experience.

� One-click access to favorite websites and a built-in favorites organizer.

� Immersive full-screen mode.

� Prioritizes user convenience.

� Caters to users seeking a user-friendly web browser with advanced features. ";

	public SetupInit()
	{
		InitializeComponent();
		DataContext = this;
		Uc_Viewmodel = new UC_Viewmodel();
	}

	public string IntroMessageProperty => IntroMessage;

	private void Setup_Click(object sender, RoutedEventArgs e)
	{
		_ = Frame.Navigate(typeof(SetupUser));
	}

	private async void RestoreNow_Click(object sender, RoutedEventArgs e)
	{
		await ShowRestoreBackupDialogAsync();
	}

	private async Task ShowRestoreBackupDialogAsync()
	{
		RestoreBackupDialog dlg = new() { XamlRoot = XamlRoot };
		_ = await dlg.ShowAsync();
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		LoginAndShowUploadWindow();
	}

	public async Task LoginAndShowUploadWindow()
	{
		try
		{
			// Execute the login command and await its completion
			await Uc_Viewmodel.LoginToMicrosoftCommand.ExecuteAsync(null);

			// Check if login was successful
			if (Uc_Viewmodel.IsMsLogin)
			{
				// Create and show the upload window
				UpLoadBackup backup = new UpLoadBackup();
				backup.Activate();
			}
			else
			{
				// Handle login failure
				ContentDialog dialog = new ContentDialog
				{
					Title = "Login Failed",
					Content = "Unable to log in. Please try again.",
					CloseButtonText = "OK",
					XamlRoot = this.XamlRoot,
				};

				await dialog.ShowAsync();
			}
		}
		catch (Exception ex)
		{
			// Handle any exceptions that occurred during login
			ContentDialog dialog = new ContentDialog
			{
				Title = "Error",
				Content = $"An error occurred: {ex.Message}",
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot,
			};

			await dialog.ShowAsync();
		}
	}
}