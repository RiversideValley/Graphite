using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Graphite.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Graphite.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Generic;

namespace Graphite.Pages
{
	public sealed partial class WebContent : Page
	{
		public WebView2 _webView;
		private bool _isInitialized;
		private string _selectionText;

		public string Title { get; private set; } = string.Empty;
		public string CurrentUrl { get; private set; } = string.Empty;
		public string FaviconUrl { get; private set; } = string.Empty;

		public WebContent()
		{
			this.InitializeComponent();
			_webView = WebViewElement;
			_webView.EnsureCoreWebView2Async();
			WebViewElement.NavigationCompleted += WebView_NavigationCompleted;
			this.Loaded += WebContent_Loaded;
			NavigateToUrl("https://www.google.com");
		}

		private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
		{
			args.Handled = true;
			_selectionText = args.ContextMenuTarget.SelectionText;

			var options = new FlyoutShowOptions()
			{
				Position = args.Location,
				ShowMode = FlyoutShowMode.Standard
			};

			OpenLinks.Visibility = args.ContextMenuTarget.HasLinkUri ? Visibility.Visible : Visibility.Collapsed;

			Ctx.ShowAt(WebViewElement, options);
		}

		private void WebContent_Loaded(object sender, RoutedEventArgs e)
		{
			_isInitialized = true;
			UpdateTabIconIfNeeded();
		}

		private async void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			Title = await _webView.CoreWebView2.ExecuteScriptAsync("document.title") ?? string.Empty;
			Title = Title.Trim('"');
			CurrentUrl = _webView.Source?.ToString() ?? string.Empty;

			var sourceUrl = _webView.Source?.ToString() ?? string.Empty;
			if (!string.IsNullOrEmpty(sourceUrl))
			{
				FaviconUrl = $"https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={sourceUrl}";
			}

			UpdateTabIconIfNeeded();
			UpdateTabTitle();
			ProgressLoading.Visibility = Visibility.Collapsed;
		}

		private void UpdateTabIconIfNeeded()
		{
			if (!_isInitialized || string.IsNullOrEmpty(FaviconUrl))
				return;

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
			await WebViewElement.EnsureCoreWebView2Async();
			Title = string.Empty;
			UpdateTabTitle();
			WebViewElement.Source = new Uri(url);
			ProgressLoading.Visibility = Visibility.Visible;
		}

		public async Task UnloadContent()
		{
			await _webView.CoreWebView2.TrySuspendAsync();
			offlinePage.Visibility = Visibility.Visible;
			Grid.Visibility = Visibility.Collapsed;
		}

		public async Task ReloadContent()
		{
			_webView.CoreWebView2.Resume();
			offlinePage.Visibility = Visibility.Collapsed;
			Grid.Visibility = Visibility.Visible;
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

		
		private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is AppBarButton button)
			{
				switch (button.Tag.ToString())
				{
					case "MenuBack":
						if (_webView.CanGoBack) _webView.GoBack();
						break;
					case "Forward":
						if (_webView.CanGoForward) _webView.GoForward();
						break;
					case "Share":
						ShareUrl(CurrentUrl, Title);
						break;
					case "Print":
						break;
					case "Copy":
						CopyToClipboard(_selectionText);
						break;
					case "Select":
						await _webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('selectAll', false, null);");
						break;
					case "Save":
						await SavePageAsPdf();
						break;
					case "Source":
						_webView.CoreWebView2.OpenDevToolsWindow();
						break;
					case "Taskmgr":
						_webView.CoreWebView2.OpenTaskManagerWindow();
						break;
				}
			}
			Ctx.Hide();
		}

		private void ShareUrl(string url, string title)
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(url);
			dataPackage.Properties.Title = title;
			Clipboard.SetContent(dataPackage);
			// Implement share UI here
		}

		private void CopyToClipboard(string text)
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);
		}

		private async Task SavePageAsPdf()
		{
			var savePicker = new Windows.Storage.Pickers.FileSavePicker();
			savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
			savePicker.FileTypeChoices.Add("PDF File", new List<string>() { ".pdf" });
			savePicker.SuggestedFileName = "WebPage";

			var file = await savePicker.PickSaveFileAsync();
			if (file != null)
			{
			}
		}

		private void ContextClicked_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem item)
			{
				switch (item.Tag.ToString())
				{
					case "OpenInTab":
						// Implement open in new tab functionality
						break;
					case "OpenInWindow":
						// Implement open in new window functionality
						break;
					case "OpenInPop":
						// Implement open in popup functionality
						break;
					case "Read":
						// Implement read aloud functionality
						break;
					case "WebApp":
						// Implement create web app functionality
						break;
				}
			}
			Ctx.Hide();
		}

		private void PermissionToggle_Click(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleMenuFlyoutItem toggleItem)
			{
				string permissionType = toggleItem.Tag.ToString();
				bool isAllowed = toggleItem.IsChecked;

				// Implement permission management here
			}
		}
	}
}

