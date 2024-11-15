using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Runtime.Exceptions;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services;
public partial class DownloadService : ObservableObject, IServiceDownloads
{

	[ObservableProperty]
	private User _authorizedUser;
	public ObservableCollection<Riverside.Graphite.Controls.DownloadItem> DownloadItemControls { get; set; }
	public event EventHandler<DownloadItemStatusEventArgs> Handler_DownItemsChange;
	public DownloadService()
	{
		_ = Intialize();
	}
	private async Task Intialize()
	{
		AuthorizedUser = AuthService.CurrentUser;
		DownloadItemControls = await GetDownloadItems();
	}

	[RelayCommand]
	public async Task RemoveDownloadControl(string fileName)
	{
		_ = await DeleteAsync(fileName);
	}

	[RelayCommand]
	public void OpenDownloadContol(string fileName)
	{
		try
		{
			_ = Process.Start("explorer.exe", "/select, " + fileName);
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
		}
	}
	private async Task<ObservableCollection<Riverside.Graphite.Controls.DownloadItem>> GetDownloadItems()
	{
		ObservableCollection<Riverside.Graphite.Controls.DownloadItem> uiControl = new();

		try
		{
			DownloadActions downloadActions = new(AuthorizedUser.Username);
			List<Riverside.Graphite.Data.Core.Models.DownloadItem> items = await downloadActions.GetAllDownloadItems();

			if (items.Count > 0)
			{
				items.ForEach(t =>
				{
					Riverside.Graphite.Controls.DownloadItem downloadItem = new(t.current_path);
					downloadItem.ServiceDownloads = this;
					uiControl.Insert(0, downloadItem);
				});
			};


		}
		catch (Exception ex)
		{
			// Handle any exceptions, such as file access or database errors
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error accessing database: {ex.Message}");
		}

		return uiControl;

	}

	#region CRUD 

	public Task SaveAsync(string FileName)
	{
		throw new System.NotImplementedException();
	}

	public async Task<bool> DeleteAsync(string FilePath)
	{
		try
		{
			File.Delete(FilePath);
			DownloadActions downloadActions = new(AuthorizedUser.Username);
			await downloadActions.DeleteDownloadItem(FilePath);
			DownloadItemControls = await GetDownloadItems();
			OnPropertyChanged(nameof(DownloadItemControls));
			Handler_DownItemsChange?.Invoke(this, new DownloadItemStatusEventArgs() { Status = DownloadItemStatusEventArgs.EnumStatus.Removed });
			return true;
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);

		}

		return false;

	}

	public async Task UpdateAsync()
	{
		DownloadItemControls = await GetDownloadItems();
		OnPropertyChanged(nameof(DownloadItemControls));
		Handler_DownItemsChange?.Invoke(this, new DownloadItemStatusEventArgs() { Status = DownloadItemStatusEventArgs.EnumStatus.Updated });
	}

	public async Task InsertAsync(string current_path, string end_time, long start_time)
	{
		try
		{
			DownloadActions downloadActions = new(AuthService.CurrentUser.Username);
			await downloadActions.InsertDownloadItem(Guid.NewGuid().ToString(), current_path, end_time, start_time);
			await UpdateAsync();
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);

		}

	}

	#endregion
}