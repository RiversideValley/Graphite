namespace Riverside.Graphite.Pages.Models
{
	public record TrendingListItem(string webSearchUrl, string name, string url, string text);

	public class TrendingItem
	{
		public string webSearchUrl { get; set; }
		public string name { get; set; }
		public string url { get; set; }
		public string text { get; set; }
		public TrendingItem() { }
		public TrendingItem(string _webSearchUrl, string _name, string _url, string _text)
		{
			webSearchUrl = _webSearchUrl;
			name = _name;
			url = _url;
			text = _text;
		}
	}
}
