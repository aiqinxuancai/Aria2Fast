using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal class AnimeSummaryTranslatorManager
    {
        private const string kSystemMessage = """
            # 角色
            你是一个只做翻译的引擎。

            ## 任务
            将用户提供的日文动漫简介翻译为简体中文。

            ## 输出格式
            只返回 JSON 字符串，格式如下：
            {"summary":"<翻译后的简介>"}

            ## 规则
            - 不输出任何解释或多余文本。
            - 保留专有名词与作品名，不添加额外内容。
            - 如果输入为空，返回 {"summary":""}。
            """;

        private const string kCacheFileName = "AnimeSummaryTranslateCache.json";
        private static readonly string kCacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kCacheFileName);
        private static readonly object _cacheLock = new();
        private static readonly Regex kJapaneseRegex = new Regex(@"[\u3040-\u309F\u30A0-\u30FF\uFF66-\uFF9F]", RegexOptions.Compiled);
        private static Dictionary<string, string> _cache = LoadCache();

        public static bool IsLikelyJapanese(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return kJapaneseRegex.IsMatch(text);
        }

        public static bool TryGetCachedTranslation(string source, out string translation)
        {
            lock (_cacheLock)
            {
                return _cache.TryGetValue(source, out translation);
            }
        }

        public static async Task<string> TranslateToChineseAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            if (TryGetCachedTranslation(text, out var cached) && !string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            try
            {
                var responseText = await AiProviderClient.SendAsync(text, kSystemMessage);
                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    JObject root = JObject.Parse(responseText);
                    var summary = (string)root["summary"];
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        summary = summary.Trim();
                        CacheTranslation(text, summary);
                        return summary;
                    }
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
            }

            return string.Empty;
        }

        private static Dictionary<string, string> LoadCache()
        {
            try
            {
                if (File.Exists(kCacheFilePath))
                {
                    var json = File.ReadAllText(kCacheFilePath);
                    var cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
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

            return new Dictionary<string, string>();
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

        private static void CacheTranslation(string source, string translation)
        {
            lock (_cacheLock)
            {
                _cache[source] = translation;
                SaveCache();
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
}
