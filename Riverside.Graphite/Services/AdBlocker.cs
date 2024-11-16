using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services
{
	public class AdBlockerWrapper : IDisposable
	{
		private bool _isEnabled;
		private HashSet<string> _adDomains;
		private List<Regex> _adPatterns;
		private HashSet<string> _whitelist;
		private WebView2 _webView;
		private string _scriptJS = default;
		private bool disposedValue;

		public AdBlockerWrapper()
		{
			_isEnabled = false;
			_adDomains = new HashSet<string>();
			_adPatterns = new List<Regex>();
			_whitelist = new HashSet<string>();
			LoadEasyList(); 
		}

		private void LoadEasyList()
		{
			string easyListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "EasyList.txt");
			if (File.Exists(easyListPath))
			{
				foreach (var line in File.ReadLines(easyListPath))
				{
					ProcessRule(line);
				}
			}
			else
			{
				Console.WriteLine("EasyList.txt not found in Assets folder.");
			}

			string jsScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "adblocker.js");
			if (File.Exists(jsScript))
			{
				foreach (var line in File.ReadLines(jsScript))
				{
					_scriptJS += line;
				}
				_scriptJS.Trim();
			}
			else
			{
				Console.WriteLine("Adblocker.js not found in Assets folder.");
			}

		}

		private void ProcessRule(string rule)
		{
			rule = rule.Trim();

			// Ignore comments
			if (rule.StartsWith("!"))
				return;

			// Handle exception rules
			if (rule.StartsWith("@@"))
			{
				_whitelist.Add(rule.Substring(2));
				return;
			}

			// Handle domain anchors
			if (rule.StartsWith("||"))
			{
				string domain = rule.Substring(2).Split('^')[0];
				_adDomains.Add(domain);
				return;
			}

			// Handle regular expression rules
			if (rule.StartsWith("/") && rule.EndsWith("/"))
			{
				_adPatterns.Add(new Regex(rule.Trim('/'), RegexOptions.Compiled));
				return;
			}

			// Handle other rules (treat as patterns)
			if (!string.IsNullOrWhiteSpace(rule))
			{
				_adPatterns.Add(new Regex(Regex.Escape(rule).Replace("\\*", ".*"), RegexOptions.Compiled));
			}
		}

		~AdBlockerWrapper()
		{
			Dispose(false); 
		}

		public void Unregister()
		{
			_webView.CoreWebView2.NavigationStarting -= WebView_NavigationStarting;
			_webView.CoreWebView2.FrameNavigationStarting -= WebView_FrameNavigationStarting;
			_webView.CoreWebView2.WebResourceRequested -= WebView_WebResourceRequested;
			_webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
			_webView.CoreWebView2Initialized -= WebView_CoreWebView2Initialized;
		}

		public async Task Initialize(WebView2 webView)
		{
			try
			{
				_webView = webView;
				
				await _webView.EnsureCoreWebView2Async();

				_webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
				_webView.CoreWebView2.FrameNavigationStarting += WebView_FrameNavigationStarting;
				_webView.CoreWebView2.WebResourceRequested += WebView_WebResourceRequested;
				_webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
				_webView.CoreWebView2Initialized += WebView_CoreWebView2Initialized;
				_webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				throw;
			}


		}

		private async void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			if (args.Exception is Exception ex) { 
				ExceptionLogger.LogException(ex);	
			}
			await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_scriptJS); 
		}

		private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			await Task.Delay(2400);
			await InjectAdBlockingScript();
		}

		public void Toggle()
		{
			_isEnabled = !_isEnabled;
			Console.WriteLine($"Ad blocker is now {(_isEnabled ? "enabled" : "disabled")}");
		}

		private async void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
		{
			if (_isEnabled)
			{
				await BlockAdsIfNecessary(e.Uri, e);
			}
		}

		private async void WebView_FrameNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
		{
			if (_isEnabled)
			{
				await BlockAdsIfNecessary(e.Uri, e);
			}
		}

		private async void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
		{
			if (_isEnabled)
			{
				await BlockAdsIfNecessary(e.Request.Uri, e);
			}
		}

		private Task BlockAdsIfNecessary(string uriString, dynamic e)
		{
			Uri uri = new Uri(uriString);

			// Check whitelist first
			if (_whitelist.Any(rule => Regex.IsMatch(uriString, WildcardToRegex(rule))))
			{
				return Task.CompletedTask; 
			}

			if (_adDomains.Contains(uri.Host) || _adPatterns.Any(pattern => pattern.IsMatch(uriString)))
			{
				if (e is CoreWebView2NavigationStartingEventArgs navEvent)
				{
					navEvent.Cancel = true;
				}
				else if (e is CoreWebView2WebResourceRequestedEventArgs resourceEvent)
				{
					resourceEvent.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", null);
				}
				Console.WriteLine($"Blocked ad from: {uri.Host}");
			}
			return Task.CompletedTask;
		}

		private string WildcardToRegex(string pattern)
		{
			return "^" + Regex.Escape(pattern)
					   .Replace("\\*", ".*")
					   .Replace("\\?", ".")
				   + "$";
		}

		public async Task InjectAdBlockingScript()
		{
			if (_isEnabled && _scriptJS != null)
			{
				await _webView.CoreWebView2.ExecuteScriptAsync(_scriptJS);
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