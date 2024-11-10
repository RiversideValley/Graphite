using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebViewAdBlocker
{
    public class AdBlocker
    {
        private bool _isEnabled;
        private HashSet<string> _adDomains;
        private List<Regex> _adPatterns;
        private HashSet<string> _whitelist;
        private CoreWebView2 _webView;

        public AdBlocker()
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

        public void Initialize(CoreWebView2 webView)
        {
            _webView = webView;
            _webView.NavigationStarting += WebView_NavigationStarting;
            _webView.FrameNavigationStarting += WebView_FrameNavigationStarting;
            _webView.WebResourceRequested += WebView_WebResourceRequested;

            // Enable WebResourceRequested event for all resource types
            _webView.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        }

        public void Toggle()
        {
            _isEnabled = !_isEnabled;
            Console.WriteLine($"Ad blocker is now {(_isEnabled ? "enabled" : "disabled")}");
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (_isEnabled)
            {
                BlockAdsIfNecessary(e.Uri, e);
            }
        }

        private void WebView_FrameNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (_isEnabled)
            {
                BlockAdsIfNecessary(e.Uri, e);
            }
        }

        private void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (_isEnabled)
            {
                BlockAdsIfNecessary(e.Request.Uri, e);
            }
        }

        private void BlockAdsIfNecessary(string uriString, dynamic e)
        {
            Uri uri = new Uri(uriString);

            // Check whitelist first
            if (_whitelist.Any(rule => Regex.IsMatch(uriString, WildcardToRegex(rule))))
            {
                return;
            }

            if (_adDomains.Contains(uri.Host) || _adPatterns.Any(pattern => pattern.IsMatch(uriString)))
            {
                if (e is CoreWebView2NavigationStartingEventArgs navEvent)
                {
                    navEvent.Cancel = true;
                }
                else if (e is CoreWebView2WebResourceRequestedEventArgs resourceEvent)
                {
                    resourceEvent.Response = _webView.Environment.CreateWebResourceResponse(null, 403, "Blocked", null);
                }
                Console.WriteLine($"Blocked ad from: {uri.Host}");
            }
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
            if (_isEnabled && _webView != null)
            {
                string script = @"
                    (function() {
                        const adSelectors = [
                            '.ad', '#ad', '[class*=""ad-""]', '[id*=""ad-""]',
                            'ins.adsbygoogle', '.advert', '.advertisement'
                            // Add more selectors as needed
                        ];
                        
                        function removeAds() {
                            adSelectors.forEach(selector => {
                                document.querySelectorAll(selector).forEach(el => el.remove());
                            });
                        }

                        removeAds();
                        new MutationObserver(removeAds).observe(document.body, {
                            childList: true,
                            subtree: true
                        });
                    })();
                ";

                await _webView.ExecuteScriptAsync(script);
            }
        }
    }
}