// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core;

public class HistoryContext : DbContext
{
	public DbSet<DbHistoryItem> Urls { get; set; }
	public DbSet<Collection> Collections { get; set; }
	public DbSet<CollectionName> CollectionNames { get; set; }

	public string ConnectionPath { get; set; }

	public HistoryContext(string username)
	{
		ConnectionPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Database", "History.db");
	}

	public static readonly string InformationFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "sql_event_logger.log");
	
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={ConnectionPath}");
        optionsBuilder.LogTo(Console.WriteLine);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collection>()
               .HasOne<DbHistoryItem>()
               .WithMany()
               .HasForeignKey(c => c.HistoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Collection>()
            .HasOne<CollectionName>()
            .WithMany()
            .HasForeignKey(c => c.CollectionNameId)
            .OnDelete(DeleteBehavior.Cascade);

            
    }

}
