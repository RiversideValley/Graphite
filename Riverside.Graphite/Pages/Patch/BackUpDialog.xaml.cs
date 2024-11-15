using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Runtime.Licensing;
using System;
using System.IO;

namespace Riverside.Graphite.Pages.Patch
{
	public sealed partial class BackUpDialog : ContentDialog
	{
		private const int MaxBackupCountStandard = 6;
		private const int MaxBackupCountPremium = 50;
		public bool IsBackupAllowed { get; set; } = true;
		public bool IsPremiumUser { get; set; } = false;
		private readonly string premiumLicensePath;
		private bool IsCloudBackup { get; set; } = false;

		private readonly Riverside.Graphite.Runtime.Licensing.AddonManager _addonManager;

		public BackUpDialog()
		{
			InitializeComponent();
			premiumLicensePath = Path.Combine(AppContext.BaseDirectory, "premium.license");
			IsPremiumUser = CheckPremiumStatus();
			CheckBackupLimit();
			_addonManager = new AddonManager();
		}

		private bool CheckPremiumStatus()
		{
			IsPremiumUser = File.Exists(premiumLicensePath);

			return IsPremiumUser;
		}

		private void CheckBackupLimit()
		{
			string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string backupDirectory = Path.Combine(documentsFolder);

			if (Directory.Exists(backupDirectory))
			{
				string[] backupFiles = Directory.GetFiles(backupDirectory, "*.firebackup");
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

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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



				_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
			}
			catch (Exception)
			{
				;
			}
		}

		private void BackupLocationToggle_Toggled(object sender, RoutedEventArgs e)
		{
			IsCloudBackup = BackupLocationToggle.IsOn;
		}
	}
}