using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml.Controls;


namespace FireBrowserWinUi3.Controls
{

    public sealed partial class DownloadListView : ListView
    {
        private DownloadService ServiceDownloads { get; }
        public DownloadListView()
        {
            ServiceDownloads = App.GetService<DownloadService>();
            this.InitializeComponent();

        }
    }
}
