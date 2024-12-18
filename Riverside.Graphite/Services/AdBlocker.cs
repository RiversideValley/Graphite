using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services
{
	public sealed class AdBlockerWrapper
	{
		private bool _isEnabled;
		private WebView2 _webView;
		private bool disposedValue;
		private string GraphiteBlocker { get; set; }
		public AdBlockerWrapper()
		{
			_isEnabled = false;
			LoadScript().GetAwaiter();
		}

		private async Task LoadScript()
		{
			GraphiteBlocker = await LoadFileHelper.LoadFileAsync(new Uri("ms-appx:///Assets/WebView/AdBlock/adblocker.js"));
		}
		public void Unregister()
		{
			if (_webView is not null && _webView.CoreWebView2 is not null)
			{
				_webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
				_webView.CoreWebView2Initialized -= WebView_CoreWebView2Initialized;
			}
		}

		public async Task Initialize(WebView2 webView)
		{
			try
			{
				_webView = webView;

				await _webView.EnsureCoreWebView2Async();

				_webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
				_webView.CoreWebView2Initialized += WebView_CoreWebView2Initialized;
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				throw;
			}
		}

		private async void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			if (args.Exception is Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
			_ = await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GraphiteBlocker);
		}

		private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			await InjectAdBlockingScript();
		}

		public void Toggle(bool onOff)
		{
			_isEnabled = onOff;
			Console.WriteLine($"Ad blocker is now {(_isEnabled ? "enabled" : "disabled")}");
		}

		public async Task InjectAdBlockingScript()
		{
			try
			{
				await _webView?.EnsureCoreWebView2Async();

				if (_isEnabled)
				{
					_ = await _webView?.CoreWebView2.ExecuteScriptAsync(GraphiteBlocker);
				}
			}
			catch (Exception e)
			{
				ExceptionLogger.LogException(e);
			}
		}
	}
}
