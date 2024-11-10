// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using FireBrowserWinUi3DataCore.Models;
using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;

namespace FireBrowserWinUi3DataCore;

public class HistoryContext : DbContext
{
    public DbSet<HistoryItem> Urls { get; set; }
    //public DbSet<DownloadItem> Downloads { get; set; }
    //public DbSet<DbUser> Users { get; set; }

    public string ConnectionPath { get; set; }
    public IEnumerable<object> HistoryEntries { get; set; }

    public HistoryContext(string username)
    {
        ConnectionPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Database", "History.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={ConnectionPath}");

}
