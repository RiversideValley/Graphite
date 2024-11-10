using FireBrowserWinUi3DataCore.Models;
using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FireBrowserWinUi3DataCore;

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
