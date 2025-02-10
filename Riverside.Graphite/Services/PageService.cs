using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Pages;
using Riverside.Graphite.Pages.TimeLinePages;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.ViewModels;
using Riverside.Graphite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Riverside.Graphite.Services
{
	public class PageService : IPageService
	{
		public readonly Dictionary<string, Type> Pages = new Dictionary<string, Type>();

		public PageService()
		{
			Configure<HomeViewModel, NewTab>();
			Configure<DownloadsViewModel, DownloadsTimeLine>(); 
			Configure<FavoritesViewModel, FavoritesTimeLine>();
			Configure<CollectionsPageViewModel, CollectionsPage>(); 
		}

		Dictionary<string, Type> IPageService.Pages => Pages;

		public Type GetPageType(string key)
		{
			Type pageType;
			lock (Pages)
			{
				if (!Pages.TryGetValue(key, out pageType))
				{
					throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
				}
			}

			return pageType;
		}

		private void Configure<VM, V>()
			where VM : CommunityToolkit.Mvvm.ComponentModel.ObservableRecipient
			where V : Page
		{
			lock (Pages)
			{
				var key = typeof(VM).FullName;
				if (Pages.ContainsKey(key))
				{
					throw new ArgumentException($"The key {key} is already configured in PageService");
				}

				var type = typeof(V);
				if (Pages.Any(p => p.Value == type))
				{
					throw new ArgumentException($"This type is already configured with key {Pages.First(p => p.Value == type).Key}");
				}

				Pages.Add(key, type);
			}
		}
	}
}