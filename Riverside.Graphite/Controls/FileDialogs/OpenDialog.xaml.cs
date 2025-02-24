using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Controls.FileDialogs	
{
	public sealed partial class OpenDialog : Window
	{
		private StorageFolder _currentFolder;
		private StorageFile _selectedFile;

		public OpenDialog()
		{
			this.InitializeComponent();

			// Set up file type filter
			FileTypeComboBox.Items.Add("All Files (*.*)");
			FileTypeComboBox.Items.Add("Text Files (*.txt)");
			FileTypeComboBox.Items.Add("Document Files (*.docx)");
			FileTypeComboBox.SelectedIndex = 0;
		}

		public async Task<StorageFile> ShowAsync(StorageFolder initialFolder = null)
		{
			_currentFolder = initialFolder ?? KnownFolders.DocumentsLibrary;
			await NavigateToFolder(_currentFolder);

			this.Activate();

			var tcs = new TaskCompletionSource<StorageFile>();
			this.Closed += (s, e) => tcs.TrySetResult(_selectedFile);

			return await tcs.Task;
		}

		private async Task NavigateToFolder(StorageFolder folder)
		{
			_currentFolder = folder;
			FileListView.Items.Clear();

			var items = await folder.GetItemsAsync();
			foreach (var item in items)
			{
				FileListView.Items.Add(item.Name);
			}
		}

		private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var selectedItemName = e.ClickedItem as string;
			var selectedItem = await _currentFolder.TryGetItemAsync(selectedItemName);

			if (selectedItem is StorageFolder folder)
			{
				await NavigateToFolder(folder);
			}
			else if (selectedItem is StorageFile file)
			{
				FileNameBox.Text = file.Name;
				_selectedFile = file;
			}
		}

		private async void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(FileNameBox.Text))
			{
				var dialog = new ContentDialog
				{
					Title = "Error",
					Content = "Please select a file.",
					CloseButtonText = "OK"
				};
				await dialog.ShowAsync();
				return;
			}

			_selectedFile = await _currentFolder.GetFileAsync(FileNameBox.Text);
			this.Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedFile = null;
			this.Close();
		}

		private async void OpenLocationButton_Click(object sender, RoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchFolderAsync(_currentFolder);
		}
	}
}