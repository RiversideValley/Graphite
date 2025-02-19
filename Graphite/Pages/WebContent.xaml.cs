using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Graphite.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;

namespace Graphite.Pages
{
	public sealed partial class WebContent : Page
	{
		private WebView2 _webView;
		private bool _isInitialized;

		public string Title { get; private set; } = string.Empty;
		public string CurrentUrl { get; private set; } = string.Empty;
		public string FaviconUrl { get; private set; } = string.Empty;

		public WebContent()
		{
			this.InitializeComponent();
			_webView = new WebView2();
			_webView.EnsureCoreWebView2Async();
			_webView.NavigationCompleted += WebView_NavigationCompleted;
			Content = _webView;
			NavigateToUrl("https://www.google.com/");
			this.Loaded += WebContent_Loaded;
		}

		private void WebContent_Loaded(object sender, RoutedEventArgs e)
		{
			_isInitialized = true;
			UpdateTabIconIfNeeded();
		}

		private async void WebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
		{
			Title = await _webView.CoreWebView2.ExecuteScriptAsync("document.title") ?? string.Empty;
			Title = Title.Trim('"'); // Remove any surrounding quotes
			CurrentUrl = _webView.Source?.ToString() ?? string.Empty;

			// Update favicon URL using Google's favicon service
			var sourceUrl = _webView.Source?.ToString() ?? string.Empty;
			if (!string.IsNullOrEmpty(sourceUrl))
			{
				FaviconUrl = $"https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={sourceUrl}";
			}

			UpdateTabIconIfNeeded();
			UpdateTabTitle();
		}

		private void UpdateTabIconIfNeeded()
		{
			if (!_isInitialized || string.IsNullOrEmpty(FaviconUrl))
				return;

			// Use DispatcherQueue to ensure we're on the UI thread
			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
			{
				if (this.Parent is Frame frame && frame.Parent is GraphiteTabViewItem tab)
				{
					var container = FindParent<GraphiteTabViewContainer>(this);
					if (container?.TabManager != null)
					{
						container.TabManager.UpdateTabIcon(tab, FaviconUrl);
					}
				}
			});
		}

		public async Task NavigateToUrl(string url)
		{
			await _webView.EnsureCoreWebView2Async();
			Title = string.Empty;
			UpdateTabTitle();
			_webView.Source = new Uri(url);
		}

		public async Task UnloadContent()
		{
			_webView.CoreWebView2.TrySuspendAsync();
		}

		public async Task ReloadContent()
		{
			_webView.CoreWebView2.Resume();
		}

		private T FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			var parent = VisualTreeHelper.GetParent(child);
			while (parent != null && !(parent is T))
			{
				parent = VisualTreeHelper.GetParent(parent);
			}
			return parent as T;
		}

		private void UpdateTabTitle()
		{
			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
			{
				if (this.Parent is Frame frame && frame.Parent is GraphiteTabViewItem tab)
				{
					tab.Header = string.IsNullOrEmpty(Title) ? "New Tab" : Title;
					
				}
			});
		}
	}
}

