namespace FireBrowserWinUi3DataCore.Models.Contacts;
public interface IDownloadItem
{

    int id { get; set; }
    string guid { get; set; }
    string current_path { get; set; }
    string end_time { get; set; }
    long start_time { get; set; }

}
