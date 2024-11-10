using FireBrowserWinUi3DataCore.Actions.Contracts;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FireBrowserWinUi3DataCore.Actions;
public class SettingsActions : IUISettings
{
    public SettingsActions(string username)
    {
        SettingsContext = new SettingsContext(username);
    }

    public SettingsContext SettingsContext { get; set; }

    public async Task<Settings> GetSettingsAsync()
    {
        try
        {
            var settings = await SettingsContext.Settings.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync();
            return settings;
        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
            Console.WriteLine($"Settings Database failed to gather data: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> UpdateSettingsAsync(Settings settings)
    {
        try
        {
            SettingsContext.ChangeTracker.Clear();
            SettingsContext.Settings.Update(settings);
            var result = await SettingsContext.SaveChangesAsync();
            return result > 0 ? true : false;

        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
            Console.WriteLine($"Settings Database failed to Save data: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> InsertUserSettingsAsync(Settings settings)
    {
        try
        {
            SettingsContext.Settings.Add(settings);
            var result = await SettingsContext.SaveChangesAsync();
            return result > 0 ? true : false;

        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
            Console.WriteLine($"Settings Database failed to Save data: {ex.Message}");
            return false;
        }
    }
}