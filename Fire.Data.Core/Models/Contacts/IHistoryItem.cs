namespace FireBrowserWinUi3DataCore.Models.Contacts;

public interface IHistoryItem
{
    int id { get; set; }
    string last_visit_time { get; set; }
    string url { get; set; }
    string title { get; set; }
    int visit_count { get; set; }
    int typed_count { get; set; }
    int hidden { get; set; }

}
