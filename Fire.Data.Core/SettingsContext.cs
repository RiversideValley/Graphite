using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace FireBrowserWinUi3DataCore;
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
        if (username == null)
        {
            throw new ArgumentNullException(nameof(username));
        }
        else
        {
            ConnectionPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Settings", "Settings.db");
        }
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={ConnectionPath}");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //modelBuilder.Entity<Settings>().HasData(new FireBrowserWinUi3MultiCore.Settings(true).Self);
    }
}
