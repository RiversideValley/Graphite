using Microsoft.Graph.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Riverside.Graphite.Services
{
	public class AdBlockerWrapper : IDisposable
	{
		private bool _isEnabled;
		private HashSet<string> _adDomains;
		private List<Regex> _adPatterns;
		private HashSet<string> _whitelist;
		private WebView2 _webView;
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
			await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GraphiteBlocker); 
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
			try
			{
				await _webView.EnsureCoreWebView2Async();

				if (_isEnabled)
				{
					await _webView.CoreWebView2.ExecuteScriptAsync(GraphiteBlocker);
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
		#region jscode
		private const string GraphiteBlocker = @"(function () {
							var selectors = [
								'#sidebar-wrap', '#advert', '#xrail', '#middle-article-advert-container',
								'#sponsored-recommendations', '#around-the-web', '#sponsored-recommendations',
								'#taboola-content', '#taboola-below-taboola-native-thumbnails', '#inarticle_wrapper_div',
								'#rc-row-container', '#ads', '#at-share-dock', '#at4-share', '#at4-follow', '#right-ads-rail',
								'div#ad-interstitial', 'div#advert-article', 'div#ac-lre-player-ph',
								'.ad', '.avert', '.avert__wrapper', '.middle-banner-ad', '.advertisement',
								'.GoogleActiveViewClass', '.advert', '.cns-ads-stage', '.teads-inread', '.ad-banner',
								'.ad-anchored', '.js_shelf_ads', '.ad-slot', '.antenna', '.xrail-content',
								'.advertisement__leaderboard', '.ad-leaderboard', '.trc_rbox_outer', '.ks-recommended',
								'.article-da', 'div.sponsored-stories-component', 'div.addthis-smartlayers',
								'div.article-adsponsor', 'div.signin-prompt', 'div.article-bumper', 'div.video-placeholder',
								'div.top-ad-container', 'div.header-ad', 'div.ad-unit', 'div.demo-block', 'div.OUTBRAIN',
								'div.ob-widget', 'div.nwsrm-wrapper', 'div.announcementBar', 'div.partner-resources-block',
								'div.arrow-down', 'div.m-ad', 'div.story-interrupt', 'div.taboola-recommended',
								'div.ad-cluster-container', 'div.ctx-sidebar', 'div.incognito-modal', '.OUTBRAIN', '.subscribe-button',
								'.ads9', '.leaderboards', '.GoogleActiveViewElement', '.mpu-container', '.ad-300x600', '.tf-ad-block',
								'.sidebar-ads-holder-top', '.ads-one', '.FullPageModal__scroller',
								'.content-ads-holder', '.widget-area', '.social-buttons', '.ac-player-ph',
								'script', 'iframe', 'video', 'aside#sponsored-recommendations', 'aside[role=banner]', 'aside',
								'amp-ad', 'span[id^=ad_is_]', 'div[class*=indianapolis-optin]', 'div[id^=google_ads_iframe]',
								'div[data-google-query-id]', 'section[data-response]', 'ins.adsbygoogle', 'div[data-google-query-id]',
								'div[data-test-id=fullPageSignupModal]', 'div[data-test-id=giftWrap]'];
	
							function findAllShadowRoots(root = document)
								{
									const elementsWithShadowRoot = [];
									const traverse = (node) => {
										if (node.shadowRoot)
										{
											elementsWithShadowRoot.push(node);
										}
										node.childNodes.forEach(child => {
											if (child.nodeType === Node.ELEMENT_NODE)
											{
												traverse(child);
											}
										});
									}
									traverse(root);
									return elementsWithShadowRoot;
								}

								function graphiteRemoveAds()
								{
									let _graphiteCollection = new Array();
								for (let i in selectors)
									{
										let nodesList = document.querySelectorAll(selectors[i]);
										for (let i = 0; i < nodesList.length; i++)
										{
											let el = nodesList[i];
											if (el && el.parentNode)
											{
												var out = {
													'message': 'Ad removal by graphite browser', 
												'element' : el
												}
												_graphiteCollection.push(out);
												el.parentNode.removeChild(el);
											}
										}
									}
									console.dir(_graphiteCollection);
									graphiteShadow();
								}

								function matchExcluded(element, selectors) { return selectors.some(selector => { return element.parentNode.querySelectorAll(selector).includes(element); }); }
								function graphiteShadow()
								{

									const shadowRootElements = findAllShadowRoots();

									let _graphiteCollection = new Array();
									selectors.map(selector => document.querySelector(selector)).filter(el => el !== null);


									let nodesList = shadowRootElements;
									let shadowChild = [];
									for (let a in nodesList)
									{
										let el = nodesList[a];
										const traverse = (node) => {

											node.childNodes.forEach(child => {
												if (child.nodeType === Node.ELEMENT_NODE)
												{
													traverse(child);
												}
												else
												{
													shadowChild.push(child);
												}

											});
										};

										if (el.shadowRoot)
										{
											if (el.shadowRoot.childNodes)
												el.shadowRoot.childNodes.forEach(child => traverse(child));
										}
									}

									// ie: if a list doesn't have a function assign a function from another type.. 
									if (NodeList.prototype.includes === undefined) { NodeList.prototype.includes = Array.prototype.includes; }

									var shadowMatches = Array.from(shadowChild).filter(element => matchExcluded(element, selectors));

									for (let i = 0; i < shadowMatches.length; i++)
									{
										let el = shadowChild[i];
										if (el && el.parentNode)
										{
											var out = {
												'message': 'Ad removal by graphite browser',
												'element': el
											}
											_graphiteCollection.push(out);
											el.parentNode.removeChild(el);
										}
									}


									console.dir(_graphiteCollection);

								}

								graphiteRemoveAds();

								new MutationObserver(graphiteRemoveAds).observe(document.body, {
								childList: true,
								subtree: true,
							});

						})();";
		#endregion
	}
}
