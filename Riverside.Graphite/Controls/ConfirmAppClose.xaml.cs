using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Riverside.Graphite.Core;
using Riverside.Graphite.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace Riverside.Graphite.Controls;

public sealed partial class ConfirmAppClose : ContentDialog
{
	public SettingsService SettingsService { get; set; }

	public ConfirmAppClose()
	{
		this.InitializeComponent();
		SettingsService = App.GetService<SettingsService>();
	}

	private async void DontShowMeAgain_Checked(object sender, RoutedEventArgs e)
	{
		if (sender is CheckBox checkBox)
		{
			try
			{
				// Update the setting
				SettingsService.CoreSettings.ConfirmCloseDlg = checkBox.IsChecked.Value;

				// Save the modified settings back to the user's settings file
				await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
			}
			catch (Exception ex)
			{
				// Log the error or show a message to the user
				Debug.WriteLine($"Error saving settings: {ex.Message}");
				// You might want to reset the checkbox if saving failed
				checkBox.IsChecked = !checkBox.IsChecked;
			}
		}
	}
}
