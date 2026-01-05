using Aria2Fast.Service.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal class AnimeAiReviewManager
    {
        private const string kCacheFileName = "AnimeAiReviewCache.json";
        private static readonly string kCacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kCacheFileName);
        private static readonly object _cacheLock = new();
        private static readonly Dictionary<string, AnimeAiReviewCacheItem> _cache = LoadCache();

        private const string kSystemMessage = """
            # 角色
            你是一个严谨的动漫简评编辑。

            ## 任务
            根据提供的标题与简介生成简短中文评论，并给出 1-10 分评分。

            ## 内容要求
            - 从热度、原作背景、口碑三个角度写简评。
            - 不要编造奖项或事实；若未提供相关信息，请明确写“信息不足”。
            - 简评 3-5 句话，保持自然、克制、可信。

            ## 输出格式
            只返回 JSON 字符串，格式如下：
            {"score":8.5,"review":"..."}

            ## 约束
            - 不输出任何解释或多余文本。
            - 分数范围 1-10，可含 1 位小数。
            """;

        public static bool TryGetCachedReview(string key, out AnimeAiReviewCacheItem item)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out item))
                {
                    if ((DateTime.Now - item.CachedAt).TotalDays < 30)
                    {
                        return true;
                    }
                    _cache.Remove(key);
                }
            }
            item = null;
            return false;
        }

        public static async Task<AnimeAiReviewCacheItem?> GetReviewAsync(string title, string summary, TmdbAnimeInfo? tmdbInfo)
        {
            var key = BuildKey(title, summary, tmdbInfo?.Id);
            if (TryGetCachedReview(key, out var cached))
            {
                return cached;
            }

            var prompt = BuildUserPrompt(title, summary, tmdbInfo);
            var responseText = await AiProviderClient.SendAsync(prompt, kSystemMessage);
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return null;
            }

            if (!TryParseReview(responseText, out var review, out var score))
            {
                return null;
            }

            var item = new AnimeAiReviewCacheItem
            {
                Review = review,
                Score = score,
                CachedAt = DateTime.Now
            };

            lock (_cacheLock)
            {
                _cache[key] = item;
                SaveCache();
            }

            return item;
        }

        private static string BuildKey(string title, string summary, int? tmdbId)
        {
            return $"{title}|{tmdbId?.ToString() ?? "0"}|{summary}".Trim();
        }

        private static string BuildUserPrompt(string title, string summary, TmdbAnimeInfo? tmdbInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"标题：{title}");
            if (!string.IsNullOrWhiteSpace(summary))
            {
                sb.AppendLine($"简介：{summary}");
            }

            if (tmdbInfo != null)
            {
                sb.AppendLine($"TMDB评分：{tmdbInfo.VoteAverage:F1}/10");
                sb.AppendLine($"TMDB评价数：{tmdbInfo.VoteCount}");
                if (!string.IsNullOrWhiteSpace(tmdbInfo.FirstAirDate))
                {
                    sb.AppendLine($"首播：{tmdbInfo.FirstAirDate}");
                }
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

        private static Dictionary<string, AnimeAiReviewCacheItem> LoadCache()
        {
            try
            {
                if (File.Exists(kCacheFilePath))
                {
                    var json = File.ReadAllText(kCacheFilePath);
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
                var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(kCacheFilePath, json);
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
            }
        }

        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
            }

            try
            {
                if (File.Exists(kCacheFilePath))
                {
                    File.Delete(kCacheFilePath);
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
    }
}
