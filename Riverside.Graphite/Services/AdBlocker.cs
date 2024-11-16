using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text;

namespace Riverside.Graphite.Services
{
	public partial class AdBlockerWrapper : IDisposable
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

		~AdBlockerWrapper()
		{
			Dispose(false);
		}

		private async Task  LoadScript() {

			GraphiteBlocker = await LoadFileHelper.LoadFileAsync(new Uri("ms-appx:///Assets/WebView/AdBlock/adblocker.js")); 
		}
		public void Unregister()
		{
			_webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
			_webView.CoreWebView2Initialized -= WebView_CoreWebView2Initialized;
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
			await Task.Delay(320);
			await InjectAdBlockingScript();
		}

		public void Toggle()
		{
			_isEnabled = !_isEnabled;
			Console.WriteLine($"Ad blocker is now {(_isEnabled ? "enabled" : "disabled")}");
		}

		public async Task InjectAdBlockingScript()
		{
			try
			{
				await _webView.EnsureCoreWebView2Async();

				if (_isEnabled)
				{
					_ = await _webView.CoreWebView2.ExecuteScriptAsync(GraphiteBlocker);
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Unregister();
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		
	}
}
