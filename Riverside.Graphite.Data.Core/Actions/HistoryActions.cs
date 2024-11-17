using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite.Data.Core.Actions.Contracts;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions;

public class HistoryActions : IHistoryActions
{
	public HistoryContext HistoryContext { get; set; }

	public HistoryActions(string username)
	{
		HistoryContext = new HistoryContext(username);
	}

	public async Task InsertHistoryItem(string url, string title, int visitCount, int typedCount, int hidden)
	{
		try
		{
			if (await HistoryContext.Urls.FirstOrDefaultAsync(t => t.url == url) is DbHistoryItem item)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				_ = HistoryContext.Urls.Where(x => x.url == url).ExecuteUpdate(y => y.SetProperty(z => z.visit_count, z => z.visit_count + 1)
				.SetProperty(a => a.last_visit_time, dateTime)
				.SetProperty(b => b.title, title));
			}
			else
			{
				_ = await HistoryContext.Urls.AddAsync(new DbHistoryItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), url, title, visitCount, typedCount, hidden));
			}

			_ = await HistoryContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error inserting history item: {ex.Message}");
		}
	}

	public async Task DeleteHistoryItem(string url)
	{
		try
		{
			_ = await HistoryContext.Urls.Where(x => x.url == url).ExecuteDeleteAsync();
			_ = await HistoryContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error deleting history item: {ex.Message}");
		}
	}

	public async Task DeleteAllHistoryItems()
	{
		Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tran = HistoryContext.Database.BeginTransaction();

		try
		{
			// executesqlAsync doesn't have a default tran so add on.
			_ = await HistoryContext.Database.ExecuteSqlAsync($"DELETE FROM urls;");
			_ = await HistoryContext.SaveChangesAsync();
			tran.Commit();
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error deleting history item: {ex.Message}");
			tran.Rollback();
		}
	}

	public async Task<ObservableCollection<HistoryItem>> GetAllHistoryItems()
	{
		try
		{
			List<HistoryItem> items = (from x in HistoryContext.Urls
									   select new HistoryItem
									   {
										   Id = x.hidden,
										   Url = x.url,
										   Title = x.title,
										   VisitCount = x.visit_count,
										   TypedCount = x.typed_count,
										   LastVisitTime = x.last_visit_time,
										   Hidden = x.hidden,
										   ImageSource = new BitmapImage(new Uri($"https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={x.url}&size=32"))
									   }).OrderBy(x => x.LastVisitTime).Reverse().ToList();

			return await Task.FromResult(items.ToObservableCollection<HistoryItem>());
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
			Console.WriteLine($"Error gathering History Items: {ex.Message}");
			return await Task.FromResult(new ObservableCollection<HistoryItem>());
		}
	}
}