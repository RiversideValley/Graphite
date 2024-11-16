using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions.Contracts;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions;
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
			Settings settings = await SettingsContext.Settings.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync();
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
			_ = SettingsContext.Settings.Update(settings);
			int result = await SettingsContext.SaveChangesAsync();
			return result > 0;
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
			_ = SettingsContext.Settings.Add(settings);
			int result = await SettingsContext.SaveChangesAsync();
			return result > 0;
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Settings Database failed to Save data: {ex.Message}");
			return false;
		}
	}
}