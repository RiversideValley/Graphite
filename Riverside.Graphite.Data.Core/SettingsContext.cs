using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using System;
using System.IO;

namespace Riverside.Graphite.Data.Core;
// https://andywatt83.medium.com/testing-entity-framework-migrations-9bc5dc25190b
//https://www.thinktecture.com/en/entity-framework-core/changing-db-migration-schema-at-runtime-in-2-1/
//https://www.milanjovanovic.tech/blog/efcore-migrations-a-detailed-guide
// sql servere compare schema extensions 
// https://learn.microsoft.com/en-us/azure-data-studio/extensions/schema-compare-extension?view=sql-server-ver16
public class SettingsContext : DbContext
{
	public DbSet<Settings> Settings { get; set; }
	public string ConnectionPath { get; set; }
	public SettingsContext(string username)
	{
		ConnectionPath = username == null
			? throw new ArgumentNullException(nameof(username))
			: Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Settings", "Settings.db");
	}
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		_ = optionsBuilder.UseSqlite($"Data Source={ConnectionPath}");
	}
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		//modelBuilder.Entity<Settings>().HasData(new Riverside.Graphite.Core.Settings(true).Self);20240923053656_InitSettingsModelSnapShot
	}
}
