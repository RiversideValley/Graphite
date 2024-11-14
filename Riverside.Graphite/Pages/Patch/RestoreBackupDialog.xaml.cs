using Riverside.Graphite.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using WinRT.Interop;

namespace Riverside.Graphite.Pages.Patch
{
	public sealed partial class RestoreBackupDialog : ContentDialog
	{
		public string SelectedBackupPath { get; private set; }
		public bool CancelledByUser { get; set; }
		public RestoreBackupDialog()
		{
			InitializeComponent();
		}

		private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
		{
			LoadBackupFiles();
		}

		private async void LoadBackupFiles()
		{
			StorageFolder documentsFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			System.Collections.Generic.IReadOnlyList<StorageFile> backupFiles = await documentsFolder.GetFilesAsync();

			System.Collections.Generic.List<BackupFileInfo> fireBackupFiles = backupFiles
				.Where(file => file.FileType.Equals(".firebackup", StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(file => file.DateCreated)
				.Select(file => new BackupFileInfo(file.Name, file.Path))
				.ToList();

			BackupListBox.ItemsSource = fireBackupFiles;
		}

		private async void RestartAndCloseWindows()
		{

			// close whatever window is open ie: setup, usercentral --> need to give control back to windowscontroller....


			Hide();

			if (CancelledByUser) { return; }

			AppService.ActiveWindow?.Close();

			// is main is open give use option on next start up... 
			if (App.Current.m_window is not null)
			{

				IntPtr hWnd = WindowNative.GetWindowHandle(App.Current.m_window);

				if (hWnd != IntPtr.Zero)
				{
					ContentDialog dlg = new();
					dlg.PrimaryButtonText = "Restart";
					dlg.SecondaryButtonText = "Cancel";
					dlg.Content = new TextBlock().Text = "You need to restart the application in order to restore your backup.\n\nIf you choose (not) to restart your FireBrowser then the RESTORE will happen automically the next time your start the application";
					dlg.PrimaryButtonClick += (s, e) =>
					{
						_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
					};
					dlg.XamlRoot = XamlRoot;
					_ = await dlg.ShowAsync(ContentDialogPlacement.Popup);

				}

			}



		}
		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (BackupListBox.SelectedItem is BackupFileInfo selectedBackup)
			{
				SelectedBackupPath = selectedBackup.FilePath;
				string tempFolderPath = Path.GetTempPath();
				string restoreFilePath = Path.Combine(tempFolderPath, "restore.fireback");

				// Write the selected backup file path to the restore.fireback file
				await File.WriteAllTextAsync(restoreFilePath, SelectedBackupPath);

				RestartAndCloseWindows();

			}
			else
			{
				args.Cancel = true;
				// Show an error message or InfoBar indicating that no backup was selected
			}
		}

		private void BackupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateRestoreButtonState();
		}

		private void ConfirmCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
		{
			UpdateRestoreButtonState();
		}

		private void UpdateRestoreButtonState()
		{
			IsPrimaryButtonEnabled = BackupListBox.SelectedItem != null && ConfirmCheckBox.IsChecked == true;
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			CancelledByUser = true;
			RestartAndCloseWindows();
		}
	}

	public class BackupFileInfo
	{
		public string FileName { get; }
		public string FilePath { get; }

		public BackupFileInfo(string fileName, string filePath)
		{
			FileName = fileName;
			FilePath = filePath;
		}

		public override string ToString()
		{
			return FileName;
		}
	}
}