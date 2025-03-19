using System;

namespace Riverside.Graphite.Controls;

public class TabState
{
	public Guid Id { get; set; }
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

	// Add group properties
	public string GroupName { get; set; }
	public string GroupColor { get; set; }
}


