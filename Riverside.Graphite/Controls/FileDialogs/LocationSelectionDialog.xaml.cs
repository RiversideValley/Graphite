using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Controls.FileDialogs
{
	public sealed partial class LocationSelectionDialog : Window
	{
		private StorageFolder _currentFolder;
		private StorageFolder _flyoutCurrentFolder;
		private TaskCompletionSource<StorageFolder> _tcs;
		private ObservableCollection<StorageFolder> _knownFolders;
		private ObservableCollection<StorageFolder> _flyoutFolders;

		public LocationSelectionDialog()
		{
			this.InitializeComponent();

			// Set up custom title bar
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);

			_knownFolders = new ObservableCollection<StorageFolder>();
			_flyoutFolders = new ObservableCollection<StorageFolder>();
			KnownFoldersListView.ItemsSource = _knownFolders;
			FlyoutFolderListView.ItemsSource = _flyoutFolders;

			InitializeFolders();
		}

		private async void InitializeFolders()
		{
			await InitializeKnownFolders();
			await InitializeFlyoutFolders();
		}

		private async Task InitializeKnownFolders()
		{
			_knownFolders.Clear();
			await AddStandardFolder(_knownFolders, Environment.SpecialFolder.UserProfile, "Desktop", "Desktop");
			await AddStandardFolder(_knownFolders, Environment.SpecialFolder.UserProfile, "Documents", "Documents");
			await AddStandardFolder(_knownFolders, Environment.SpecialFolder.UserProfile, "Music", "Music");
			await AddStandardFolder(_knownFolders, Environment.SpecialFolder.UserProfile, "Pictures", "Pictures");
			await AddStandardFolder(_knownFolders, Environment.SpecialFolder.UserProfile, "Videos", "Videos");
			await AddStandardFolder(_knownFolders, Environment.SpecialFolder.UserProfile, "Downloads", "Downloads");
		}

		private async Task AddStandardFolder(ObservableCollection<StorageFolder> folders, Environment.SpecialFolder specialFolder, string folderName, string displayName)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(specialFolder), folderName);
				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				folders.Add(folder);
			}
			catch (Exception)
			{
				// If the folder is not accessible, skip it
			}
		}

		private async Task InitializeFlyoutFolders()
		{
			string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			_flyoutCurrentFolder = await StorageFolder.GetFolderFromPathAsync(userProfilePath);
			await UpdateFlyoutFolderList();
		}

		private async Task UpdateFlyoutFolderList()
		{
			try
			{
				_flyoutFolders.Clear();
				var folders = await _flyoutCurrentFolder.GetFoldersAsync();
				foreach (var folder in folders.OrderBy(f => f.Name))
				{
					_flyoutFolders.Add(folder);
				}
				CurrentFolderTextBlock.Text = _flyoutCurrentFolder.Path;
			}
			catch (UnauthorizedAccessException)
			{
				await ShowErrorDialog("Access denied. Unable to list folder contents.");
			}
			catch (Exception ex)
			{
				await ShowErrorDialog($"An error occurred: {ex.Message}");
			}
		}

		private async Task ShowErrorDialog(string message)
		{
			ContentDialog errorDialog = new ContentDialog
			{
				Title = "Error",
				Content = message,
				CloseButtonText = "OK"
			};
			await errorDialog.ShowAsync();
		}

		private void KnownFoldersListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is StorageFolder clickedFolder)
			{
				_currentFolder = clickedFolder;
				CurrentFolderTextBlock.Text = _currentFolder.Path;
			}
		}

		private async void FlyoutFolderListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is StorageFolder clickedFolder)
			{
				_flyoutCurrentFolder = clickedFolder;
				await UpdateFlyoutFolderList();
			}
		}

		private async void ParentFolderButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var parentFolder = await _flyoutCurrentFolder.GetParentAsync();
				if (parentFolder != null)
				{
					_flyoutCurrentFolder = parentFolder;
					await UpdateFlyoutFolderList();
				}
			}
			catch (UnauthorizedAccessException)
			{
				await ShowErrorDialog("Access denied. Unable to access parent folder.");
			}
			catch (Exception ex)
			{
				await ShowErrorDialog($"An error occurred: {ex.Message}");
			}
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			// The flyout will open automatically due to the XAML definition
		}

		private void CancelFlyoutButton_Click(object sender, RoutedEventArgs e)
		{
			BrowseButton.Flyout.Hide();
		}

		private void SaveFlyoutButton_Click(object sender, RoutedEventArgs e)
		{
			_currentFolder = _flyoutCurrentFolder;
			CurrentFolderTextBlock.Text = _currentFolder.Path;
			BrowseButton.Flyout.Hide();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			_tcs?.TrySetResult(null);
			this.Close();
		}

		private void SelectButton_Click(object sender, RoutedEventArgs e)
		{
			_tcs?.TrySetResult(_currentFolder);
			this.Close();
		}

		public Task<StorageFolder> GetSelectedFolderAsync()
		{
			_tcs = new TaskCompletionSource<StorageFolder>();
			return _tcs.Task;
		}
	}
}