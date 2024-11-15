using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Bing.WebSearch;
using Microsoft.Bing.WebSearch.Models;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Riverside.Graphite.Runtime.Helpers
{
	public partial class SdkBingWebSearch : ObservableObject
	{
		private string ApiKey => "29948d69f0294a5a9b8b75831dd06c8a";
		internal WebSearchClient WebSearchClient { get; set; }
		public List<string> FiltersJsonPropsSearchResponse { get; set; }
		public string FilterBy { get; set; }

		public SdkBingWebSearch()
		{
			WebSearchClient = new WebSearchClient(new ApiKeyServiceClientCredentials(ApiKey));
			FiltersJsonPropsSearchResponse = JsonReflection.JsonGetterPropNames.GetJsonPropertyNames<SearchResponse>();

		}

		public async Task<SearchResponse> WebSearchResultTypesLookup(string queryText)
		{
			if (string.IsNullOrEmpty(queryText)) return null;

			try
			{
				var webData = await WebSearchClient.Web.SearchAsync(query: queryText.Trim());
				if (webData != null)
					return webData;
			}

			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}

			return null;

		}

		public async Task<SearchResponse> WebResultsWithCountAndOffset(string queryText, int offSet, int resultCount)
		{
			if (string.IsNullOrEmpty(queryText)) return null;

			try
			{
				var webData = await WebSearchClient.Web.SearchAsync(query: queryText, offset: offSet, count: resultCount);
				if (webData != null) return webData;
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
			return null;
		}
		public async Task<SearchResponse> WebSearchWithResponseFilter(string queryText)
		{
			if (string.IsNullOrEmpty(FilterBy)) return null;
			if (string.IsNullOrEmpty(queryText)) return null;

			try
			{
				IList<string> responseFilterstrings = new List<string>() { FilterBy };
				var webData = await WebSearchClient.Web.SearchAsync(query: queryText, responseFilter: responseFilterstrings);
				if (webData != null) return webData;
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
			return null;
		}
		public async Task<SearchResponse> WebSearchWithAnswerCountPromoteAndSafeSearch(string queryText)
		{
			if (string.IsNullOrEmpty(FilterBy)) return null;
			if (string.IsNullOrEmpty(queryText)) return null;

			try
			{
				IList<string> promoteAnswertypeStrings = new List<string>() { FilterBy };
				var webData = await WebSearchClient.Web.SearchAsync(query: queryText, answerCount: 2, promote: promoteAnswertypeStrings, safeSearch: SafeSearch.Strict);
				if (webData != null) return webData;

			}
			catch (Exception ex)
			{
				Console.WriteLine("Encountered exception. " + ex.Message);
			}
			return null;
		}
	}
}

