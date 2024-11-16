using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace Riverside.Graphite.Controls;
public sealed partial class DownloadFlyout : Flyout
{
	public DownloadService DownloadService { get; set; }
	public DownloadFlyout()
	{
		DownloadService = App.GetService<DownloadService>();
		// Let service tell page when changes happen always stay in sync with all download compontents. a. replace list everytime -> flyouts aren't derived from a UIElement, hence to x:Bind..
		DownloadService.Handler_DownItemsChange += (_, _) => GetDownloadItems();

		InitializeComponent();

		GetDownloadItems();
	}

	public void TriggerFlyoutUpdate()
	{
		DownloadItemsListView.Items.Clear();
		GetDownloadItems();
	}

	public async void GetDownloadItems()
	{
		try
		{
			if (AuthService.CurrentUser is not Riverside.Graphite.Core.User)
				return;

			if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Database", "Downloads.db")))
			{
				try
				{
					var db = new DatabaseServices();
					_ = await db.DatabaseCreationValidation();
					_ = await db.InsertUserSettings();
				}
				catch (Exception)
				{
					throw;
				}
			}

			DownloadItemsListView.Items.Clear();
			DownloadActions downloadActions = new(AuthService.CurrentUser.Username);
			List<Riverside.Graphite.Data.Core.Models.DownloadItem> items = await downloadActions.GetAllDownloadItems();

			if (items.Count > 0)
			{
				items.ForEach(t =>
				{
					DownloadItem downloadItem = new(t.current_path);
					downloadItem.ServiceDownloads = DownloadService;
					DownloadItemsListView.Items.Insert(0, downloadItem);
				});
			}
		}
		catch (Exception ex)
		{
			// Handle any exceptions, such as file access or database errors
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error accessing database: {ex.Message}");
		}
	}

	private void ShowDownloads_Click(object sender, RoutedEventArgs e)
	{
		string downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
		string pathToExecutable = @"C:\Windows\explorer.exe";
		string arguments = string.Empty;

		_ = ProcessStarter.StartProcess(pathToExecutable, arguments, $"{downloadPath}");
	}

	private void OpenDownloadsItem_Click(object sender, RoutedEventArgs e)
	{
		MainWindow window = (Application.Current as App)?.m_window as MainWindow;
		window.UrlBox.Text = "firebrowser://downloads";
		_ = window.TabContent.Navigate(typeof(Riverside.Graphite.Pages.TimeLinePages.MainTimeLine));
	}
}
