using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.Helpers;

public class TabState
{
	public string Header { get; set; }
	public bool IsSleeping { get; set; }
	public string Url { get; set; }
	public bool IsSplitView { get; set; }
	public string FaviconUrl { get; set; }
	public DateTime LastAccessTime { get; set; }
	public int ScrollPosition { get; set; }
	public string PageTitle { get; set; }
	public bool IsPinned { get; set; }
	public string CustomColor { get; set; }
	public bool IsNewTab { get; set; }
	public string PageType { get; set; } // Add this property to track the page type

	public bool IsWebContent => !string.IsNullOrEmpty(Url) && PageType == "WebContent";
}
