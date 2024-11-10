using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fire.Browser.Core;
using Fire.Core.Exceptions;
using Fire.Data.Core.Actions;
using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3.Services.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services;
public partial class DownloadService : ObservableObject, IServiceDownloads
{

    [ObservableProperty]
    private User _authorizedUser;
    public ObservableCollection<FireBrowserWinUi3.Controls.DownloadItem> DownloadItemControls { get; set; }
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
        await DeleteAsync(fileName);
    }

    [RelayCommand]
    public void OpenDownloadContol(string fileName)
    {
        try
        {
            Process.Start("explorer.exe", "/select, " + fileName);
        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
        }
    }
    private async Task<ObservableCollection<FireBrowserWinUi3.Controls.DownloadItem>> GetDownloadItems()
    {
        ObservableCollection<FireBrowserWinUi3.Controls.DownloadItem> uiControl = new ObservableCollection<FireBrowserWinUi3.Controls.DownloadItem>();

        try
        {
            DownloadActions downloadActions = new(AuthorizedUser.Username);
            List<Fire.Data.Core.Models.DownloadItem> items = await downloadActions.GetAllDownloadItems();

            if (items.Count > 0)
            {
                items.ForEach(t =>
                {
                    FireBrowserWinUi3.Controls.DownloadItem downloadItem = new(t.current_path);
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
            DownloadActions downloadActions = new DownloadActions(AuthorizedUser.Username);
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
            DownloadActions downloadActions = new DownloadActions(AuthService.CurrentUser.Username);
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