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
        // 使用公共只读 API Key（类似 Jellyfin/Emby 的做法）
        // 用户可以替换为自己的 Key 以获得更高的请求限制
        private const string kTmdbApiKey = "8d6d91941230c398d3a4f755e8469886"; // 默认公共 Key
        private const string kCacheFileName = "tmdb_cache.json";
        private static readonly string kCacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kCacheFileName);

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
                        api_key = kTmdbApiKey,
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
                        api_key = kTmdbApiKey,
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
                if (language.StartsWith("zh"))
                {
                    animeInfo.OverviewZh = overview;
                }
                else
                {
                    animeInfo.OverviewEn = overview;
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
    }
}
