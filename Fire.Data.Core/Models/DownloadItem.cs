using FireBrowserWinUi3DataCore.Models.Contacts;
using System.ComponentModel.DataAnnotations;

namespace FireBrowserWinUi3DataCore.Models;
public class DownloadItem : IDownloadItem
{
    [Key]
    public int id { get; set; }
    public string guid { get; set; }
    public string current_path { get; set; }
    public string end_time { get; set; }
    public long start_time { get; set; }

    public DownloadItem(string guid, string current_path, string end_time, long start_time)
    {
        this.guid = guid;
        this.current_path = current_path;
        this.end_time = end_time;
        this.start_time = start_time;
    }
}

/*
 * 
    CreateSettingsFile(user.Username);
    CreateDatabaseFile(user.Username, "History.db", @"CREATE TABLE IF NOT EXISTS urls (
                                    id INTEGER PRIMARY KEY,
                                    last_visit_time TEXT,
                                    url TEXT,
                                    title TEXT,
                                    visit_count INTEGER NOT NULL DEFAULT 0,
                                    typed_count INTEGER NOT NULL DEFAULT 0,
                                    hidden INTEGER NOT NULL DEFAULT 0
                                )");
    CreateDatabaseFile(user.Username, "Downloads.db", @"CREATE TABLE IF NOT EXISTS downloads (
                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    guid VARCHAR(50) NOT NULL,
                                    current_path TEXT NOT NULL,
                                    end_time INTEGER NOT NULL,
                                    start_time INTEGER NOT NULL
                                )");
 */
