using CommunityToolkit.Mvvm.ComponentModel;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Riverside.Graphite.Runtime.Helpers
{
	public partial class BingSearchApi : ObservableObject
	{
		[ObservableProperty]
		private string _SearchQuery;
		private string _trendlist;
		public string TrendingList { get => _trendlist; set => SetProperty(ref _trendlist, value); }

		public BingSearchApi()
		{
			// TODO: Initialize TrendingList if needed
		}

		public async Task<string> TrendingListTask(string userQuery)
		{
			return TrendingList = await RunQueryAndDisplayResults(userQuery);
		}

		public async Task<string> RunQueryAndDisplayResults(string userQuery)
		{
			try
			{
				using HttpClient client = new();
				client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "29948d69f0294a5a9b8b75831dd06c8a");

				string query = $"https://api.bing.microsoft.com/v7.0/news/trendingtopics?mkt=nl-nl";
				HttpResponseMessage httpResponseMessage = await client.GetAsync(query);

				string responseContentString = await httpResponseMessage.Content.ReadAsStringAsync();
				using JsonDocument responseObjects = JsonDocument.Parse(responseContentString);

				if (responseObjects.RootElement.TryGetProperty("value", out JsonElement valueElement))
				{
					return JsonSerializer.Serialize(valueElement);
				}
				return JsonSerializer.Serialize(new List<string>());
			}
			catch (Exception e)
			{
				ExceptionLogger.LogException(e);
			}

			return null;
		}

		private static void DisplayAllRankedResults(JsonElement responseObjects)
		{
			string[] rankingGroups = new string[] { "pole", "mainline", "sidebar", "_type", "TrendingTopics" };

			foreach (string rankingName in rankingGroups)
			{
				if (responseObjects.TryGetProperty("rankingResponse", out JsonElement rankingResponse) &&
					rankingResponse.TryGetProperty(rankingName, out JsonElement rankingGroup) &&
					rankingGroup.TryGetProperty("items", out JsonElement rankingResponseItems))
				{
					foreach (JsonElement rankingResponseItem in rankingResponseItems.EnumerateArray())
					{
						if (rankingResponseItem.TryGetProperty("resultIndex", out JsonElement resultIndex) &&
							rankingResponseItem.TryGetProperty("answerType", out JsonElement answerTypeElement))
						{
							string answerType = answerTypeElement.GetString();
							switch (answerType)
							{
								case "WebPages":
									if (responseObjects.TryGetProperty("webPages", out JsonElement webPages) &&
										webPages.TryGetProperty("value", out JsonElement webPagesValue))
									{
										DisplaySpecificResults(resultIndex, webPagesValue, "WebPage", "name", "url", "displayUrl", "snippet");
									}
									break;
								case "News":
									if (responseObjects.TryGetProperty("news", out JsonElement news) &&
										news.TryGetProperty("value", out JsonElement newsValue))
									{
										DisplaySpecificResults(resultIndex, newsValue, "News", "name", "url", "description");
									}
									break;
								case "Images":
									if (responseObjects.TryGetProperty("images", out JsonElement images) &&
										images.TryGetProperty("value", out JsonElement imagesValue))
									{
										DisplaySpecificResults(resultIndex, imagesValue, "Image", "thumbnailUrl");
									}
									break;
								case "Videos":
									if (responseObjects.TryGetProperty("videos", out JsonElement videos) &&
										videos.TryGetProperty("value", out JsonElement videosValue))
									{
										DisplaySpecificResults(resultIndex, videosValue, "Video", "embedHtml");
									}
									break;
								case "RelatedSearches":
									if (responseObjects.TryGetProperty("relatedSearches", out JsonElement relatedSearches) &&
										relatedSearches.TryGetProperty("value", out JsonElement relatedSearchesValue))
									{
										DisplaySpecificResults(resultIndex, relatedSearchesValue, "RelatedSearch", "displayText", "webSearchUrl");
									}
									break;
							}
						}
					}
				}
			}
		}

		private static void DisplaySpecificResults(JsonElement resultIndex, JsonElement items, string title, params string[] fields)
		{
			if (resultIndex.ValueKind == JsonValueKind.Undefined)
			{
				foreach (JsonElement item in items.EnumerateArray())
				{
					DisplayItem(item, title, fields);
				}
			}
			else
			{
				int index = resultIndex.GetInt32();
				if (items.ValueKind == JsonValueKind.Array && index >= 0 && index < items.GetArrayLength())
				{
					DisplayItem(items[index], title, fields);
				}
				else
				{
					Console.WriteLine($"Invalid index {index} for {title}");
				}
			}
		}

		private static void DisplayItem(JsonElement item, string title, string[] fields)
		{
			string doc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "bingSearch.txt");

			try
			{
				using StreamWriter writer = new(doc, append: true);
				writer.WriteLine($"--- {title} ---");
				foreach (string field in fields)
				{
					if (item.TryGetProperty(field, out JsonElement fieldValue))
					{
						string content = $"- {field}: {fieldValue}";
						writer.WriteLine(content);
						Console.WriteLine(content);
					}
				}
				writer.WriteLine(); // Add a blank line between items
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error writing to file: {ex.Message}");
			}
		}
	}
}