using Flurl.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal static class TavilySearchService
    {
        private const string SearchEndpoint = "https://api.tavily.com/search";
        private const int DefaultTimeoutSeconds = 20;
        private const int DefaultMaxResults = 5;

        public static bool HasApiKey()
        {
            return !string.IsNullOrWhiteSpace(AppConfig.Instance.ConfigData.TavilyKey);
        }

        public static async Task<TavilySearchResponse?> SearchAsync(string query, int maxResults = DefaultMaxResults)
        {
            if (string.IsNullOrWhiteSpace(query) || !HasApiKey())
            {
                return null;
            }

            try
            {
                var response = await SearchEndpoint
                    .WithTimeout(TimeSpan.FromSeconds(DefaultTimeoutSeconds))
                    .WithHeader("Authorization", $"Bearer {AppConfig.Instance.ConfigData.TavilyKey}")
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(new
                    {
                        query,
                        topic = "general",
                        search_depth = "advanced",
                        max_results = Math.Clamp(maxResults, 1, 8),
                        include_answer = true,
                        include_raw_content = false
                    })
                    .ReceiveString();

                var root = JObject.Parse(response);
                var resultItems = (root["results"] as JArray)?
                    .OfType<JObject>()
                    .Select(item => new TavilySearchResult
                    {
                        Title = item.Value<string>("title")?.Trim() ?? string.Empty,
                        Url = item.Value<string>("url")?.Trim() ?? string.Empty,
                        Summary = item.Value<string>("content")?.Trim() ?? string.Empty,
                        Source = item.Value<string>("source")?.Trim() ?? string.Empty,
                        Score = item.Value<double?>("score") ?? 0
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Url))
                    .ToList() ?? new List<TavilySearchResult>();

                return new TavilySearchResponse
                {
                    Query = query,
                    Answer = root.Value<string>("answer")?.Trim() ?? string.Empty,
                    Results = resultItems
                };
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[Tavily] 搜索失败：{query} {ex.Message}");
                return null;
            }
        }
    }

    internal sealed class TavilySearchResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<TavilySearchResult> Results { get; set; } = new List<TavilySearchResult>();
    }

    internal sealed class TavilySearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
