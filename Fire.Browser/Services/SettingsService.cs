using CommunityToolkit.Mvvm.Messaging;
using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3.Services.Messages;
using FireBrowserWinUi3DataCore.Actions;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services;
public class SettingsService : ISettingsService
{
    #region MemberProps
    public SettingsActions Actions { get; set; }
    public User CurrentUser { get; set; }
    public Settings CoreSettings { get; set; }
    #endregion
    internal IMessenger Messenger { get; set; }
    public SettingsService()
    {
        Initialize();
        Messenger = App.GetService<IMessenger>();
    }

    public async void Initialize()
    {
        try
        {

            if (AuthService.IsUserAuthenticated)
            {
                CurrentUser = AuthService.CurrentUser ?? null;
                Actions = new SettingsActions(AuthService.CurrentUser.Username);
                CoreSettings = await Actions?.GetSettingsAsync();
            }

        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
        }
    }

    public async Task SaveChangesToSettings(User user, FireBrowserWinUi3MultiCore.Settings settings)
    {
        try
        {

            if (!AuthService.IsUserAuthenticated) return;

            AppService.AppSettings = settings;
            if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
            {
                await Actions?.SettingsContext.Database.MigrateAsync();
            }

            await Actions?.UpdateSettingsAsync(settings);
            // get new from database. 
            CoreSettings = await Actions?.GetSettingsAsync();

            var obj = new object();
            lock (obj)
            {
                Messenger?.Send(new Message_Settings_Actions(EnumMessageStatus.Settings));
            }
        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
            Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");

        }
    }
}