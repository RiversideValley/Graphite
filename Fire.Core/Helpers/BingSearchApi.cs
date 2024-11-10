using CommunityToolkit.Mvvm.ComponentModel;
using FireBrowserWinUi3Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace FireBrowserWinUi3Core.Helpers
{
    public partial class BingSearchApi : ObservableObject
    {
        /*  TODO: 
            //2024-02-20
            //TODO: add bing search api for trending to newTab.  
            // I've addes  a free bing search api subscription to my azure account 
            // key:  29948d69f0294a5a9b8b75831dd06c8a
            // curl -H "Ocp-Apim-Subscription-Key: 29948d69f0294a5a9b8b75831dd06c8a" https://api.bing.microsoft.com/v7.0/news/trendingtopics
            // make a variable $get
            // 1. $get = (Invoke-WebRequest -Uri "https://api.bing.microsoft.com/v7.0/news/trendingtopics?mkt=en-us" -Method Get -Headers @{ 'Ocp-Apim-Subscription-Key' = '29948d69f0294a5a9b8b75831dd06c8a' })
            // 2. $get is JsonObject 
            // 3. echo $get.Content 
            // 4. now lets parse it 
            // 5. $array = ($get.Content | ConvertFrom-Json) 
            // parse it by a pipe |=> convertfrom-json
            // 6. let see echo $array.value
            // 7. now what ok, lets convet to html 
            // 8. $array.value | ConvertTo-Html 
            // 9. save to a file 
            // 10. New-Item -Type File -Name "trending.html"
            // 11. Add-Content "trending.html" -Value ($array.value | ConvertTo-Html)
            // 12. Notepad .\trending.html 

            //<summary>
            //https://learn.microsoft.com/en-us/bing/search-apis/bing-web-search/tutorial/csharp-ranking-tutorial
            //https://learn.microsoft.com/en-us/bing/search-apis/bing-news-search/overview
            //https://stackoverflow.com/questions/70669970/how-to-hand-headers-in-curl-request-in-powershell-windows
            //https://stackoverflow.com/questions/50860411/read-array-from-json-and-pass-to-variable-in-powershell 
        */
        [ObservableProperty]
        private string _SearchQuery;
        private string _trendlist;
        public string TrendingList { get { return _trendlist; } set { SetProperty(ref _trendlist, value); } }


        public BingSearchApi()
        {
            // result Jobject 
            // todo parse and return to HomeViewModel to add to newTab.. 
            //TrendingList = [.. RunQueryAndDisplayResults(null).GetAwaiter().GetResult()];
        }

        public async Task<string> TrendingListTask(string userQuery)
        {
            return TrendingList = await RunQueryAndDisplayResults(userQuery);
        }
        public Task<string> RunQueryAndDisplayResults(string userQuery)
        {
            try
            {
                // Create a query
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "29948d69f0294a5a9b8b75831dd06c8a");
                //<summary>
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["q"] = userQuery;
                //var query = "https://api.bing.microsoft.com/v7.0/search?" + queryString;
                //</summary> user for a bing search 

                var query = $"https://api.bing.microsoft.com/v7.0/news/trendingtopics?mkt=nl-nl";
                // Run the query
                HttpResponseMessage httpResponseMessage = client.GetAsync(query).Result;

                // Deserialize the response content
                var responseContentString = httpResponseMessage.Content.ReadAsStringAsync().Result;
                JObject responseObjects = JObject.Parse(responseContentString);

                return Task.FromResult(Newtonsoft.Json.JsonConvert.SerializeObject(responseObjects.SelectToken("value").ToList()));
            }
            catch (Exception e)
            {
                ExceptionLogger.LogException(e);

            }

            return Task.FromResult<string>(null);
        }
        static void DisplayAllRankedResults(JsonElement responseObjects)
        {
            string[] rankingGroups = new string[] { "pole", "mainline", "sidebar", "_type", "TrendingTopics" };

            // Loop through the ranking groups in priority order
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

        static void DisplaySpecificResults(JsonElement resultIndex, JsonElement items, string title, params string[] fields)
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
        static void DisplayItem(JsonElement item, string title, string[] fields)
        {
            var doc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "bingSearch.txt");

            try
            {
                using (StreamWriter writer = new StreamWriter(doc, append: true))
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file: {ex.Message}");
            }
        }
    }
}