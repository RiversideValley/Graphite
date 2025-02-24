using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class ExtensionsSettings : Page
	{
		public ObservableCollection<Extension> WebviewExtensions { get; set; }
		public ObservableCollection<Extension> GraphiteExtensions { get; set; }

		public ExtensionsSettings()
		{
			this.InitializeComponent();
			InitializeExtensions();
		}

		private void InitializeExtensions()
		{
			// Initialize with dummy data. Replace this with actual extension loading logic.
			WebviewExtensions = new ObservableCollection<Extension>
			{
				new Extension { Name = "Webview Extension 1" },
				new Extension { Name = "Webview Extension 2" }
			};

			GraphiteExtensions = new ObservableCollection<Extension>
			{
				new Extension { Name = "Graphite Extension 1" },
				new Extension { Name = "Graphite Extension 2" }
			};

			WebviewExtensionsList.ItemsSource = WebviewExtensions;
			GraphiteExtensionsList.ItemsSource = GraphiteExtensions;
		}

		private async void AddWebviewExtension_Click(object sender, RoutedEventArgs e)
		{
			ContentDialog dialog = new ContentDialog
			{
				Title = "Add Webview Extension",
				Content = "Enter extension name:",
				PrimaryButtonText = "Add",
				CloseButtonText = "Cancel"
			};

			dialog.XamlRoot = this.XamlRoot;

			if (await dialog.ShowAsync() == ContentDialogResult.Primary)
			{
				// Add logic to actually install the extension
				WebviewExtensions.Add(new Extension { Name = "New Webview Extension" });
			}
		}

		private async void AddGraphiteExtension_Click(object sender, RoutedEventArgs e)
		{
			ContentDialog dialog = new ContentDialog
			{
				Title = "Add Graphite Extension",
				Content = "Enter extension name:",
				PrimaryButtonText = "Add",
				CloseButtonText = "Cancel"
			};

			dialog.XamlRoot = this.XamlRoot;

			if (await dialog.ShowAsync() == ContentDialogResult.Primary)
			{
				// Add logic to actually install the extension
				GraphiteExtensions.Add(new Extension { Name = "New Graphite Extension" });
			}
		}

		private void DeleteWebviewExtension_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is Extension extension)
			{
				// Add logic to actually uninstall the extension
				WebviewExtensions.Remove(extension);
			}
		}

		private void DeleteGraphiteExtension_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is Extension extension)
			{
				// Add logic to actually uninstall the extension
				GraphiteExtensions.Remove(extension);
			}
		}

		private async void EditWebviewExtension_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is Extension extension)
			{
				ContentDialog dialog = new ContentDialog
				{
					Title = "Edit Webview Extension",
					Content = "Enter new extension name:",
					PrimaryButtonText = "Save",
					CloseButtonText = "Cancel"
				};

				dialog.XamlRoot = this.XamlRoot;

				if (await dialog.ShowAsync() == ContentDialogResult.Primary)
				{
					// Add logic to actually update the extension
					extension.Name = "Updated Webview Extension";
					// Trigger UI update
					WebviewExtensionsList.ItemsSource = null;
					WebviewExtensionsList.ItemsSource = WebviewExtensions;
				}
			}
		}

		private async void EditGraphiteExtension_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is Extension extension)
			{
				ContentDialog dialog = new ContentDialog
				{
					Title = "Edit Graphite Extension",
					Content = "Enter new extension name:",
					PrimaryButtonText = "Save",
					CloseButtonText = "Cancel"
				};

				dialog.XamlRoot = this.XamlRoot;

				if (await dialog.ShowAsync() == ContentDialogResult.Primary)
				{
					// Add logic to actually update the extension
					extension.Name = "Updated Graphite Extension";
					// Trigger UI update
					GraphiteExtensionsList.ItemsSource = null;
					GraphiteExtensionsList.ItemsSource = GraphiteExtensions;
				}
			}
		}
	}

	public class Extension
	{
		public string Name { get; set; }
	}
}