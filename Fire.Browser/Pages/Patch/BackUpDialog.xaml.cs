using Fire.Core.Licensing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;

namespace FireBrowserWinUi3.Pages.Patch
{
	public sealed partial class BackUpDialog : ContentDialog
	{
		private const int MaxBackupCountStandard = 6;
		private const int MaxBackupCountPremium = 50;
		public bool IsBackupAllowed { get; set; } = true;
		public bool IsPremiumUser { get; set; } = false;
		private string premiumLicensePath;
		private bool IsCloudBackup { get; set; } = false;

		private Fire.Core.Licensing.AddonManager _addonManager;

		public BackUpDialog()
		{
			this.InitializeComponent();
			premiumLicensePath = Path.Combine(AppContext.BaseDirectory, "premium.license");
			IsPremiumUser = CheckPremiumStatus();
			CheckBackupLimit();
			_addonManager = new AddonManager();
		}

		private bool CheckPremiumStatus()
		{
			if (File.Exists(premiumLicensePath))
			{
				IsPremiumUser = true;
			}
			else
			{
				IsPremiumUser = false;
			}

			return IsPremiumUser;
		}

		private void CheckBackupLimit()
		{
			string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string backupDirectory = Path.Combine(documentsFolder);

			if (Directory.Exists(backupDirectory))
			{
				var backupFiles = Directory.GetFiles(backupDirectory, "*.firebackup");
				int maxBackupCount = IsPremiumUser ? MaxBackupCountPremium : MaxBackupCountStandard;

				if (backupFiles.Length >= maxBackupCount)
				{
					IsBackupAllowed = false;
					InfoBarBackupWarning.IsOpen = true;
					DefaultInfo.IsOpen = false;
					PrimaryButtonText = "Disabled";
				}
				else
				{
					IsBackupAllowed = true;
					InfoBarBackupWarning.IsOpen = false;
					DefaultInfo.IsOpen = true;
				}
			}
		}

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (!IsBackupAllowed)
			{
				args.Cancel = true;
				return;
			}

			try
			{
				string tempPath = Path.GetTempPath();
				string backupFilePath = Path.Combine(tempPath, "backup.fireback");

				using (StreamWriter writer = File.CreateText(backupFilePath))
				{
					writer.WriteLine(IsCloudBackup ? "cloud" : "local");
				}



				Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
			}
			catch (Exception ex)
			{

			}
		}

		private void BackupLocationToggle_Toggled(object sender, RoutedEventArgs e)
		{
			IsCloudBackup = BackupLocationToggle.IsOn;
		}
	}
}