using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Services;


namespace Riverside.Graphite.Controls
{
	public sealed partial class DownloadListView : ListView
	{
		private DownloadService ServiceDownloads { get; }
		public DownloadListView()
		{
			ServiceDownloads = App.GetService<DownloadService>();
			InitializeComponent();
		}
	}
}
