using FireBrowserWinUi3DataCore.Models.Contacts;
using System.ComponentModel.DataAnnotations;

namespace FireBrowserWinUi3DataCore.Models;

public class HistoryItem : IHistoryItem
{
    [Key]
    public int id { get; set; }
    public string last_visit_time { get; set; }
    public string url { get; set; }
    public string title { get; set; }
    public int visit_count { get; set; }
    public int typed_count { get; set; }
    public int hidden { get; set; }

    public HistoryItem(string last_visit_time, string url, string title, int visit_count, int typed_count, int hidden)
    {
        this.last_visit_time = last_visit_time;
        this.url = url;
        this.title = title;
        this.visit_count = visit_count;
        this.typed_count = typed_count;
        this.hidden = hidden;
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
