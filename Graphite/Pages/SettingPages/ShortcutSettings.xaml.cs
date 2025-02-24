using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class ShortcutSettings : Page
	{
		private Dictionary<string, string> shortcuts;

		public ShortcutSettings()
		{
			this.InitializeComponent();
			InitializeShortcuts();
		}

		private void InitializeShortcuts()
		{
			// Initialize with default shortcuts
			shortcuts = new Dictionary<string, string>
			{
				{ "back", "Alt + Left" },
				{ "forward", "Alt + Right" },
				{ "refresh", "F5" },
				{ "newtab", "Ctrl + T" },
				{ "closetab", "Ctrl + W" },
				{ "settings", "Ctrl + ," },
				{ "find", "Ctrl + F" }
			};
		}

		private async void EditShortcut_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			string shortcutKey = button.Tag.ToString();
			var currentShortcut = shortcuts[shortcutKey];

			// Create the dialog
			ContentDialog dialog = new ContentDialog
			{
				Title = "Edit Shortcut",
				Content = new TextBox
				{
					PlaceholderText = "Press new shortcut keys",
					Text = currentShortcut
				},
				PrimaryButtonText = "Save",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary
			};

			dialog.XamlRoot = this.XamlRoot;

			var result = await dialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				var textBox = dialog.Content as TextBox;
				if (textBox != null)
				{
					// Update the shortcut
					shortcuts[shortcutKey] = textBox.Text;

					// Update the UI
					var card = button.Parent as SettingsCard;
					if (card != null)
					{
						card.Description = $"Current: {textBox.Text}";
					}

					// Save the shortcuts (implement your own saving mechanism)
					SaveShortcuts();
				}
			}
		}

		private void SaveShortcuts()
		{
			// Implement your shortcut saving logic here
			// This could be saving to local settings, a file, or a database
		}
	}
}