using Aria2Fast.Service.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal class AnimeAiReviewManager
    {
        private const string CacheFileName = "AnimeAiReviewCache.json";
        private const string CacheVersion = "review-v2-tavily";
        private const int SearchResultLimit = 4;
        private const int AutoFetchPageLimit = 2;
        private const int SearchSummaryMaxLength = 280;
        private const int PageContentMaxLength = 2200;

        private static readonly string CacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CacheFileName);
        private static readonly object CacheLock = new();
        private static readonly Dictionary<string, AnimeAiReviewCacheItem> Cache = LoadCache();

        private const string SystemMessage = """
            # 角色
            你是一个谨慎、克制的动漫评论编辑。
            ## 任务
            根据提供的标题、简介和补充资料生成简短中文评论，并给出 1-10 分评分。
            如资料不足，可以先使用工具搜索和抓取网页，但不要编造未确认的事实。
            ## 内容要求
            - 从题材吸引力、改编或制作背景、口碑或热度三个角度写简评
            - 若资料明显不足，请明确写出“信息不足”
            - 简评 3-5 句，自然、克制、可信
            ## 输出格式
            只返回 JSON 字符串，格式如下：
            {"score":8.5,"review":"..."}

            ## 约束
            - 不输出任何额外解释
            - 分数范围 1-10，可保留 1 位小数
            """;

        private const string SearchToolSchema = """
            {
              "type": "object",
              "properties": {
                "query": {
                  "type": "string",
                  "description": "搜索查询词，适合检索小众动漫或动画作品信息"
                },
                "maxResults": {
                  "type": "integer",
                  "description": "返回结果数，1 到 4，默认 4"
                }
              },
              "required": [ "query" ]
            }
            """;

        private const string FetchToolSchema = """
            {
              "type": "object",
              "properties": {
                "url": {
                  "type": "string",
                  "description": "要抓取正文内容的网页 URL"
                }
              },
              "required": [ "url" ]
            }
            """;

        public static bool TryGetCachedReview(string key, out AnimeAiReviewCacheItem item)
        {
            lock (CacheLock)
            {
                if (Cache.TryGetValue(key, out item))
                {
                    if ((DateTime.Now - item.CachedAt).TotalDays < 30)
                    {
                        return true;
                    }

                    Cache.Remove(key);
                }
            }

            item = null;
            return false;
        }

        public static async Task<AnimeAiReviewCacheItem?> GetReviewAsync(string title, string summary, TmdbAnimeInfo? tmdbInfo)
        {
            bool shouldUseExternalResearch = ShouldUseExternalResearch(title, summary, tmdbInfo);
            var key = BuildKey(title, summary, tmdbInfo?.Id, shouldUseExternalResearch);
            if (TryGetCachedReview(key, out var cached))
            {
                return cached;
            }

            string responseText = string.Empty;
            if (shouldUseExternalResearch)
            {
                responseText = await TryGenerateWithFunctionToolsAsync(title, summary, tmdbInfo);
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    responseText = await TryGenerateWithAutoResearchAsync(title, summary, tmdbInfo);
                }
            }

            if (string.IsNullOrWhiteSpace(responseText))
            {
                var prompt = BuildUserPrompt(title, summary, tmdbInfo);
                responseText = await AiProviderClient.SendAsync(prompt, SystemMessage);
            }

            if (!TryParseReview(responseText, out var review, out var score))
            {
                return null;
            }

            var item = new AnimeAiReviewCacheItem
            {
                Review = review,
                Score = score,
                CachedAt = DateTime.Now,
                UsedExternalResearch = shouldUseExternalResearch
            };

            lock (CacheLock)
            {
                Cache[key] = item;
                SaveCache();
            }

            return item;
        }

        private static async Task<string> TryGenerateWithFunctionToolsAsync(string title, string summary, TmdbAnimeInfo? tmdbInfo)
        {
            if (!AiProviderClient.SupportsFunctionTools())
            {
                return string.Empty;
            }

            try
            {
                EasyLogManager.Logger.Info($"[AIReview] 尝试工具调用补充资料：{title}");
                var prompt = BuildUserPrompt(title, summary, tmdbInfo, preferToolResearch: true);
                return await AiProviderClient.SendWithToolsAsync(prompt, SystemMessage, BuildResearchTools());
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AIReview] 工具调用失败，改用应用侧检索：{title} {ex.Message}");
                return string.Empty;
            }
        }

        private static async Task<string> TryGenerateWithAutoResearchAsync(string title, string summary, TmdbAnimeInfo? tmdbInfo)
        {
            if (!TavilySearchService.HasApiKey())
            {
                return string.Empty;
            }

            try
            {
                var query = BuildSearchQuery(title, tmdbInfo);
                var searchResponse = await TavilySearchService.SearchAsync(query, SearchResultLimit);
                if (searchResponse == null || searchResponse.Results.Count == 0)
                {
                    EasyLogManager.Logger.Info($"[AIReview] Tavily 未返回有效结果：{title}");
                    return string.Empty;
                }

                EasyLogManager.Logger.Info($"[AIReview] Tavily 命中 {searchResponse.Results.Count} 条结果：{title}");

                var fetchedPages = new List<WebPageContent>();
                foreach (var result in searchResponse.Results.Take(AutoFetchPageLimit))
                {
                    var page = await WebPageFetchService.FetchAsync(result.Url, PageContentMaxLength);
                    if (page != null)
                    {
                        fetchedPages.Add(page);
                        EasyLogManager.Logger.Info($"[AIReview] 已抓取网页：{page.Url}");
                    }
                }

                var prompt = BuildUserPrompt(
                    title,
                    summary,
                    tmdbInfo,
                    externalEvidence: BuildExternalEvidence(searchResponse, fetchedPages));

                return await AiProviderClient.SendAsync(prompt, SystemMessage);
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AIReview] 应用侧检索失败：{title} {ex.Message}");
                return string.Empty;
            }
        }

        private static IReadOnlyList<AiFunctionTool> BuildResearchTools()
        {
            return new[]
            {
                new AiFunctionTool
                {
                    Name = "search_anime_info",
                    Description = "当作品较小众、已有简介很少或 TMDB 信息不够时，搜索该动漫或动画作品的公开资料。",
                    JsonSchema = SearchToolSchema,
                    InvokeAsync = SearchAnimeInfoAsync
                },
                new AiFunctionTool
                {
                    Name = "fetch_webpage_content",
                    Description = "抓取搜索结果页面的正文纯文本，用来确认作品题材、背景和口碑信息。",
                    JsonSchema = FetchToolSchema,
                    InvokeAsync = FetchWebPageContentAsync
                }
            };
        }

        private static async Task<string> SearchAnimeInfoAsync(string argumentsJson)
        {
            try
            {
                var args = JObject.Parse(argumentsJson);
                var query = args.Value<string>("query")?.Trim() ?? string.Empty;
                var maxResults = args.Value<int?>("maxResults") ?? SearchResultLimit;
                if (string.IsNullOrWhiteSpace(query))
                {
                    return "{\"error\":\"query is required\"}";
                }

                var searchResponse = await TavilySearchService.SearchAsync(query, maxResults);
                if (searchResponse == null)
                {
                    return "{\"error\":\"search failed\"}";
                }

                return JsonConvert.SerializeObject(new
                {
                    query = searchResponse.Query,
                    answer = searchResponse.Answer,
                    results = searchResponse.Results
                        .Take(SearchResultLimit)
                        .Select(item => new
                        {
                            title = item.Title,
                            url = item.Url,
                            summary = TrimText(item.Summary, SearchSummaryMaxLength),
                            source = item.Source,
                            score = item.Score
                        })
                });
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AIReview] 搜索工具失败：{ex.Message}");
                return $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}";
            }
        }

        private static async Task<string> FetchWebPageContentAsync(string argumentsJson)
        {
            try
            {
                var args = JObject.Parse(argumentsJson);
                var url = args.Value<string>("url")?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return "{\"error\":\"url is required\"}";
                }

                var page = await WebPageFetchService.FetchAsync(url, PageContentMaxLength);
                if (page == null)
                {
                    return "{\"error\":\"fetch failed\"}";
                }

                return JsonConvert.SerializeObject(new
                {
                    url = page.Url,
                    title = page.Title,
                    content = TrimText(page.Content, PageContentMaxLength)
                });
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AIReview] 网页抓取工具失败：{ex.Message}");
                return $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}";
            }
        }

        private static bool ShouldUseExternalResearch(string title, string summary, TmdbAnimeInfo? tmdbInfo)
        {
            if (string.IsNullOrWhiteSpace(title) || !TavilySearchService.HasApiKey())
            {
                return false;
            }

            if (tmdbInfo == null)
            {
                return true;
            }

            var text = summary;
            if (string.IsNullOrWhiteSpace(text))
            {
                text = tmdbInfo.GetBestOverview();
            }

            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 80)
            {
                return true;
            }

            if (tmdbInfo.VoteCount < 10 && string.IsNullOrWhiteSpace(tmdbInfo.FirstAirDate))
            {
                return true;
            }

            return false;
        }

        private static string BuildKey(string title, string summary, int? tmdbId, bool useExternalResearch)
        {
            return $"{CacheVersion}|{(useExternalResearch ? "ext" : "local")}|{title}|{tmdbId?.ToString() ?? "0"}|{summary}".Trim();
        }

        private static string BuildUserPrompt(
            string title,
            string summary,
            TmdbAnimeInfo? tmdbInfo,
            string externalEvidence = "",
            bool preferToolResearch = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"标题：{title}");

            if (!string.IsNullOrWhiteSpace(summary))
            {
                sb.AppendLine($"简介：{summary}");
            }

            if (tmdbInfo != null)
            {
                sb.AppendLine($"TMDB 标题：{tmdbInfo.Name}");
                if (!string.IsNullOrWhiteSpace(tmdbInfo.OriginalName))
                {
                    sb.AppendLine($"原名：{tmdbInfo.OriginalName}");
                }

                if (tmdbInfo.VoteAverage > 0)
                {
                    sb.AppendLine($"TMDB 评分：{tmdbInfo.VoteAverage:F1}/10");
                }

                if (tmdbInfo.VoteCount > 0)
                {
                    sb.AppendLine($"TMDB 评价数：{tmdbInfo.VoteCount}");
                }

                if (!string.IsNullOrWhiteSpace(tmdbInfo.FirstAirDate))
                {
                    sb.AppendLine($"首播：{tmdbInfo.FirstAirDate}");
                }
            }

            if (preferToolResearch)
            {
                sb.AppendLine("如果以上资料不足以判断，请先调用搜索和网页抓取工具，再输出结果。");
            }

            if (!string.IsNullOrWhiteSpace(externalEvidence))
            {
                sb.AppendLine();
                sb.AppendLine("补充外部资料：");
                sb.AppendLine(externalEvidence);
            }

            return sb.ToString().Trim();
        }

        private static string BuildSearchQuery(string title, TmdbAnimeInfo? tmdbInfo)
        {
            var parts = new List<string>();

            AddQueryPart(parts, title);
            AddQueryPart(parts, tmdbInfo?.Name);
            AddQueryPart(parts, tmdbInfo?.OriginalName);
            AddQueryPart(parts, "anime");
            AddQueryPart(parts, "动画");

            if (!string.IsNullOrWhiteSpace(tmdbInfo?.FirstAirDate) && tmdbInfo.FirstAirDate.Length >= 4)
            {
                AddQueryPart(parts, tmdbInfo.FirstAirDate.Substring(0, 4));
            }

            return string.Join(" ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static void AddQueryPart(List<string> parts, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            value = value.Trim();
            parts.Add(value.Contains(' ') ? $"\"{value}\"" : value);
        }

        private static string BuildExternalEvidence(TavilySearchResponse searchResponse, IReadOnlyList<WebPageContent> pages)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"搜索词：{searchResponse.Query}");

            if (!string.IsNullOrWhiteSpace(searchResponse.Answer))
            {
                sb.AppendLine($"搜索摘要：{TrimText(searchResponse.Answer, 400)}");
            }

            foreach (var result in searchResponse.Results.Take(SearchResultLimit))
            {
                sb.AppendLine($"搜索结果：{result.Title}");
                sb.AppendLine($"链接：{result.Url}");
                if (!string.IsNullOrWhiteSpace(result.Summary))
                {
                    sb.AppendLine($"摘要：{TrimText(result.Summary, SearchSummaryMaxLength)}");
                }
            }

            foreach (var page in pages)
            {
                sb.AppendLine($"网页标题：{page.Title}");
                sb.AppendLine($"网页链接：{page.Url}");
                sb.AppendLine($"网页正文：{TrimText(page.Content, PageContentMaxLength)}");
            }

            return sb.ToString().Trim();
        }

        private static bool TryParseReview(string responseText, out string review, out double score)
        {
            review = string.Empty;
            score = 0;

            try
            {
                var json = ExtractJson(responseText);
                var root = JObject.Parse(json);
                var reviewToken = root["review"];
                var scoreToken = root["score"];

                review = reviewToken?.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(review))
                {
                    return false;
                }

                if (scoreToken != null && double.TryParse(scoreToken.ToString(), out var parsed))
                {
                    score = Math.Clamp(parsed, 1.0, 10.0);
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
                return false;
            }
        }

        private static string ExtractJson(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                return trimmed;
            }

            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                return trimmed.Substring(start, end - start + 1);
            }

            return trimmed;
        }

        private static string TrimText(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim();
            if (text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        private static string EscapeJson(string text)
        {
            return (text ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static Dictionary<string, AnimeAiReviewCacheItem> LoadCache()
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    var json = File.ReadAllText(CacheFilePath);
                    var cache = JsonConvert.DeserializeObject<Dictionary<string, AnimeAiReviewCacheItem>>(json);
                    if (cache != null)
                    {
                        return cache;
                    }
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
            }

            return new Dictionary<string, AnimeAiReviewCacheItem>();
        }

        private static void SaveCache()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Cache, Formatting.Indented);
                File.WriteAllText(CacheFilePath, json);
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
            }
        }

        public static void ClearCache()
        {
            lock (CacheLock)
            {
                Cache.Clear();
            }

            try
            {
                if (File.Exists(CacheFilePath))
                {
                    File.Delete(CacheFilePath);
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
            }
        }
    }

    internal class AnimeAiReviewCacheItem
    {
        public string Review { get; set; } = string.Empty;
        public double Score { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.Now;
        public bool UsedExternalResearch { get; set; }
    }
}
