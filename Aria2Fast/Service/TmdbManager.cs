using Aria2Fast.Service.Model;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    /// <summary>
    /// TMDB (The Movie Database) 管理器
    /// 用于获取动漫的详细信息、评分、中文简介等
    /// </summary>
    public class TmdbManager
    {
        private static TmdbManager? _instance;
        public static TmdbManager Instance => _instance ??= new TmdbManager();

        private const string kTmdbApiBase = "https://api.themoviedb.org/3";

        // TMDB API Key 配置说明:
        // - GitHub Actions 编译: 从 GitHub Secrets 注入真实 Key
        // - 本地开发: 在 GetApiKey() 中从 TMDBConfig.json 读取
        // - 源码占位符: {TMDB_API_KEY} 会被 CI/CD 替换
        private const string kTmdbApiKeyDefault = "{TMDB_API_KEY}"; // 由 GitHub Actions 替换
        private const string kCacheFileName = "TMDBCache.json";
        private const string kConfigFileName = "TMDBConfig.json";
        private static readonly string kCacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kCacheFileName);
        private static readonly string kConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kConfigFileName);

        /// <summary>
        /// 获取 TMDB API Key（支持用户自定义）
        /// </summary>
        private string GetApiKey()
        {
            try
            {
                // 尝试从配置文件读取用户自定义的 Key
                if (File.Exists(kConfigFilePath))
                {
                    var config = JsonConvert.DeserializeObject<TmdbConfig>(File.ReadAllText(kConfigFilePath));
                    if (config != null && !string.IsNullOrWhiteSpace(config.ApiKey))
                    {
                        Debug.WriteLine("[TMDB] 使用用户自定义 API Key");
                        return config.ApiKey;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 读取配置失败: {ex.Message}");
            }

            // 检查默认 Key 是否为占位符（本地开发未替换）
            if (string.IsNullOrWhiteSpace(kTmdbApiKeyDefault))
            {
                Debug.WriteLine("[TMDB] ⚠️ API Key 未配置！请在 TMDBConfig.json 中设置 ApiKey");
                // 本地开发时，返回空字符串会导致 API 调用失败，但不会崩溃
                return string.Empty;
            }

            // 降级使用默认 Key（GitHub Actions 注入的）
            return kTmdbApiKeyDefault;
        }

        private TmdbCache _cache;
        private readonly object _cacheLock = new object();

        private TmdbManager()
        {
            _cache = LoadCache();
        }

        /// <summary>
        /// 加载缓存
        /// </summary>
        private TmdbCache LoadCache()
        {
            try
            {
                if (File.Exists(kCacheFilePath))
                {
                    var json = File.ReadAllText(kCacheFilePath);
                    var cache = JsonConvert.DeserializeObject<TmdbCache>(json);
                    if (cache != null)
                    {
                        Debug.WriteLine($"[TMDB] 加载缓存成功，共 {cache.Cache.Count} 条记录");
                        return cache;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 加载缓存失败: {ex.Message}");
            }

            return new TmdbCache();
        }

        /// <summary>
        /// 保存缓存
        /// </summary>
        private void SaveCache()
        {
            try
            {
                lock (_cacheLock)
                {
                    var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                    File.WriteAllText(kCacheFilePath, json);
                    Debug.WriteLine($"[TMDB] 保存缓存成功，共 {_cache.Cache.Count} 条记录");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 保存缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索动漫
        /// </summary>
        /// <param name="name">动漫名称</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>TMDB 动漫信息</returns>
        public async Task<TmdbAnimeInfo?> SearchAnimeAsync(string name, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            // 清理名称，移除多余字符
            var cleanName = CleanAnimeName(name);

            // 检查缓存
            if (useCache && _cache.Cache.TryGetValue(cleanName, out var cachedInfo))
            {
                // 检查缓存是否过期（30天）
                if ((DateTime.Now - cachedInfo.CachedAt).TotalDays < 30)
                {
                    Debug.WriteLine($"[TMDB] 使用缓存: {cleanName}");
                    return cachedInfo;
                }
            }

            try
            {
                Debug.WriteLine($"[TMDB] 搜索动漫: {cleanName}");

                // 先搜索动漫
                var searchUrl = $"{kTmdbApiBase}/search/tv";
                var searchResult = await searchUrl
                    .SetQueryParams(new
                    {
                        api_key = GetApiKey(),
                        query = cleanName,
                        language = "zh-CN"
                    })
                    .GetStringAsync();

                var searchJson = JObject.Parse(searchResult);
                var results = searchJson["results"] as JArray;

                if (results == null || results.Count == 0)
                {
                    Debug.WriteLine($"[TMDB] 未找到结果: {cleanName}");
                    return null;
                }

                // 获取第一个结果的 ID
                var firstResult = results[0];
                var tvId = firstResult["id"]?.Value<int>() ?? 0;

                if (tvId == 0)
                {
                    return null;
                }

                // 获取详细信息（中文）
                var animeInfo = await GetAnimeDetailsAsync(tvId, "zh-CN");

                if (animeInfo == null)
                {
                    return null;
                }

                // 如果中文简介为空，尝试获取英文简介
                if (string.IsNullOrWhiteSpace(animeInfo.OverviewZh))
                {
                    var enInfo = await GetAnimeDetailsAsync(tvId, "en-US");
                    if (enInfo != null)
                    {
                        animeInfo.OverviewEn = enInfo.OverviewEn;
                    }
                }

                // 保存到缓存
                lock (_cacheLock)
                {
                    _cache.Cache[cleanName] = animeInfo;
                }
                SaveCache();

                return animeInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 搜索动漫失败: {cleanName}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取动漫详细信息
        /// </summary>
        private async Task<TmdbAnimeInfo?> GetAnimeDetailsAsync(int tvId, string language)
        {
            try
            {
                var detailUrl = $"{kTmdbApiBase}/tv/{tvId}";
                var detailResult = await detailUrl
                    .SetQueryParams(new
                    {
                        api_key = GetApiKey(),
                        language = language
                    })
                    .GetStringAsync();

                var detailJson = JObject.Parse(detailResult);

                var animeInfo = new TmdbAnimeInfo
                {
                    Id = tvId,
                    Name = detailJson["name"]?.Value<string>() ?? string.Empty,
                    OriginalName = detailJson["original_name"]?.Value<string>() ?? string.Empty,
                    VoteAverage = detailJson["vote_average"]?.Value<double>() ?? 0,
                    VoteCount = detailJson["vote_count"]?.Value<int>() ?? 0,
                    FirstAirDate = detailJson["first_air_date"]?.Value<string>() ?? string.Empty,
                    PosterPath = detailJson["poster_path"]?.Value<string>() ?? string.Empty,
                    BackdropPath = detailJson["backdrop_path"]?.Value<string>() ?? string.Empty,
                    CachedAt = DateTime.Now
                };

                var overview = detailJson["overview"]?.Value<string>() ?? string.Empty;
                // 移除每行开头的制表符和空格
                var lines = overview.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                overview = string.Join(Environment.NewLine, lines.Select(line => line.TrimStart()));

                if (language.StartsWith("zh"))
                {
                    animeInfo.OverviewZh = overview.Trim();
                }
                else
                {
                    animeInfo.OverviewEn = overview.Trim();
                }

                return animeInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 获取动漫详细信息失败: {tvId}, 语言: {language}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清理动漫名称
        /// 移除括号内容、季数信息等
        /// </summary>
        private string CleanAnimeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            // 移除括号及其内容
            var cleaned = System.Text.RegularExpressions.Regex.Replace(name, @"\(.*?\)", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\[.*?\]", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"【.*?】", "");

            // 移除季数信息
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"第[一二三四五六七八九十\d]+季", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"Season\s*\d+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"S\d+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 移除多余空格
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Cache.Clear();
            }
            SaveCache();
            Debug.WriteLine("[TMDB] 缓存已清除");
        }

        /// <summary>
        /// 清除过期缓存（超过指定天数）
        /// </summary>
        public void ClearExpiredCache(int days = 30)
        {
            lock (_cacheLock)
            {
                var expiredKeys = _cache.Cache
                    .Where(kvp => (DateTime.Now - kvp.Value.CachedAt).TotalDays > days)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.Cache.Remove(key);
                }

                if (expiredKeys.Count > 0)
                {
                    SaveCache();
                    Debug.WriteLine($"[TMDB] 清除过期缓存 {expiredKeys.Count} 条");
                }
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            lock (_cacheLock)
            {
                return $"缓存总数: {_cache.Cache.Count}";
            }
        }

        /// <summary>
        /// 设置自定义 API Key
        /// </summary>
        /// <param name="apiKey">TMDB API Key，为空则使用默认 Key</param>
        public void SetCustomApiKey(string apiKey)
        {
            try
            {
                var config = new TmdbConfig
                {
                    ApiKey = apiKey ?? string.Empty,
                    Version = 1
                };

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(kConfigFilePath, json);
                Debug.WriteLine($"[TMDB] API Key 配置已保存");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 保存 API Key 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前使用的 API Key 类型（用于调试）
        /// </summary>
        public string GetCurrentApiKeyType()
        {
            try
            {
                if (File.Exists(kConfigFilePath))
                {
                    var config = JsonConvert.DeserializeObject<TmdbConfig>(File.ReadAllText(kConfigFilePath));
                    if (config != null && !string.IsNullOrWhiteSpace(config.ApiKey))
                    {
                        return "自定义 Key";
                    }
                }
            }
            catch { }

            return "默认公共 Key";
        }
    }
}
