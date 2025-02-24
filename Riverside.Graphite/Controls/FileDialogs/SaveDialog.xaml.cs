using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Linq;
using WinRT.Interop;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Riverside.Graphite.Controls.FileDialogs
{
	public sealed partial class SaveDialog : Window
	{
		private StorageFolder _currentFolder;
		private List<string> _fileTypes;
		private ObservableCollection<FileSystemItemWrapper> _items;

		public SaveDialog()
		{
			this.InitializeComponent();

			_items = new ObservableCollection<FileSystemItemWrapper>();
			FileListView.ItemsSource = _items;

			// Initialize file types
			InitializeFileTypes();

			// Initialize with Downloads folder
			InitializeDefaultFolder();

			// Set up custom title bar
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);
		}

		private void InitializeFileTypes()
		{
			_fileTypes = new List<string> { ".txt", ".docx", ".pdf", ".jpg", ".png", ".bin", ".bat" };
			foreach (var fileType in _fileTypes)
			{
				FileTypeComboBox.Items.Add(fileType);
			}
			FileTypeComboBox.SelectedIndex = 0;
		}

		private async void InitializeDefaultFolder()
		{
			try
			{
				_currentFolder = await StorageFolder.GetFolderFromPathAsync(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");
			}
			catch (Exception)
			{
				// Fallback to DocumentsLibrary if Downloads is not accessible
				_currentFolder = KnownFolders.DocumentsLibrary;
			}

			await NavigateToFolder(_currentFolder);
		}

		private async Task NavigateToFolder(StorageFolder folder)
		{
			_currentFolder = folder;
			_items.Clear();

			var items = await folder.GetItemsAsync();
			foreach (var item in items)
			{
				_items.Add(new FileSystemItemWrapper(item));
			}

			UpdateCurrentLocationDisplay();
		}

		private void UpdateCurrentLocationDisplay()
		{
			CurrentLocationTextBlock.Text = _currentFolder.Path;
		}

		private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var selectedItem = e.ClickedItem as FileSystemItemWrapper;

			if (selectedItem.IsFolder)
			{
				await NavigateToFolder(selectedItem.Item as StorageFolder);
			}
			else
			{
				FileNameBox.Text = selectedItem.Name;
			}
		}

		private async void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(FileNameBox.Text))
			{
				var dialog = new ContentDialog
				{
					Title = "Error",
					Content = "Please enter a file name.",
					CloseButtonText = "OK"
				};
				await dialog.ShowAsync();
				return;
			}

			string fileName = FileNameBox.Text;
			string fileType = FileTypeComboBox.SelectedItem as string;

			var savePicker = new FileSavePicker();
			InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(this));

			savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
			savePicker.SuggestedFileName = fileName;
			savePicker.FileTypeChoices.Add("All Files", new List<string> { "*" });
			foreach (var type in _fileTypes)
			{
				savePicker.FileTypeChoices.Add(type, new List<string> { type });
			}

			StorageFile file = await savePicker.PickSaveFileAsync();
			if (file != null)
			{
				// Here you would typically save the file contents
				await Windows.Storage.FileIO.WriteTextAsync(file, "File content goes here");
				this.Close();
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private async void ChangeLocationButton_Click(object sender, RoutedEventArgs e)
		{
			var locationDialog = new LocationSelectionDialog();
			locationDialog.Activate();

			var selectedFolder = await locationDialog.GetSelectedFolderAsync();
			if (selectedFolder != null)
			{
				await NavigateToFolder(selectedFolder);
			}
		}
	}

	public class FileSystemItemWrapper
	{
		public string Name { get; set; }
		public bool IsFolder { get; set; }
		public IStorageItem Item { get; set; }
		public string IconGlyph => IsFolder ? "\uE8B7" : "\uE8A5";

		public FileSystemItemWrapper(IStorageItem item)
		{
			Name = item.Name;
			IsFolder = item is StorageFolder;
			Item = item;
		}
	}
}