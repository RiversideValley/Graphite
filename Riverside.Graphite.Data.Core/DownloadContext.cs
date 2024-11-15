using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Models;
using System.IO;

namespace Riverside.Graphite.Data.Core;

public class DownloadContext : DbContext
{

	public DbSet<DownloadItem> Downloads { get; set; }
	public string ConnectionPath { get; set; }
	public DownloadContext(string username)
	{
		ConnectionPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Database", "Downloads.db");
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder.UseSqlite($"Data Source={ConnectionPath}");

}
